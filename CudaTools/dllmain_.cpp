// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"
#include <cuda.h>
#include <device_launch_parameters.h>
#include <cuda_runtime.h>
#include <device_functions.h>

#include <stdint.h>
#include <string>
#include "CudaUtil.h"

#include <algorithm>

#define DllExport   __declspec( dllexport ) 

uint16_t m_imageW;
uint16_t m_imageH;
uint16_t m_roiW;
uint16_t m_roiH;
uint16_t m_roiX;
uint16_t m_roiY;
uint16_t m_maskW;
uint16_t m_maskH;
uint16_t m_maskRows;
uint16_t m_maskCols;
uint16_t m_maxPixelValue;

uint16_t * mp_d_grayImage;
uint8_t  * mp_d_colorImage;
uint16_t * mp_d_maskImage;
uint16_t * mp_d_roiImage;

uint32_t * mp_d_histogram;
uint8_t *  mp_d_colorHistogramImage;
uint32_t   m_max_histogramBinValue;

uint8_t * mp_d_redMap;
uint8_t * mp_d_greenMap;
uint8_t * mp_d_blueMap;

uint32_t * mp_d_maskApertureSums; // 1D array, holds the aperture pixel sums

float    * mp_d_FFC_Fluor_Gc; // 1D binning-corrected array, that holds the "gain" value for each pixel for Fluorescent Flat Field Correction
float	 * mp_d_FFC_Fluor_Dc; // binning-corrected "dark" array that pairs with the "gain" array above

float    * mp_d_FFC_Lumi_Gc; // 1D binning-corrected array, that holds the "gain" value for each pixel for Luminescenct Flat Field Correction
float	 * mp_d_FFC_Lumi_Dc; // binning-corrected "dark" array that pairs with the "gain" array above
uint32_t   m_h_FFC_numElements; // this holds the size of the correction arrays (should be imageWidth * imageHeight)


bool    m_colorMapSet;
bool    m_maskSet;

CudaUtil *mp_cuda;



////////////////////////////////////////////////////////////////////////////////////////////////////
// Forward Declarations

void Call_ConvertGrayscaleToColor(uint8_t* color, uint16_t* gray, uint8_t* redMap, uint8_t* greenMap, uint8_t* blueMap,
								  uint16_t width, uint16_t height, uint16_t maxGrayValue, uint16_t scaleLower, uint16_t scaleUpper);

void Call_CopyRoiToFullImage(uint16_t* full, uint16_t* roi, uint16_t fullW, uint16_t fullH,
	uint16_t  roiX, uint16_t roiY, uint16_t roiW, uint16_t roiH);

void Call_MaskImage(uint16_t* image, uint16_t* mask, uint16_t width, uint16_t height);

void Call_FlattenImage(uint16_t* image, float* Gc, float* Dc, uint16_t width, uint16_t height);

void Call_CopyCudaArrayToD3D9Memory(uint8_t* pDest, uint8_t* pSource, uint16_t pitch, uint16_t width, uint16_t height);

void Call_ComputeHistogram_512(uint32_t* hist, const uint16_t* data, uint16_t width, uint16_t height, uint8_t maxValueBitWidth);

void Call_BuildHistogramImage_512(uint8_t* histImage, uint32_t* hist, uint16_t numBins, uint16_t width, uint16_t height, uint32_t maxBinCount);

void Call_CalcApertureSums(uint32_t* sumArray, uint16_t* image, uint16_t* mask, uint16_t width, uint16_t height);

////////////////////////////////////////////////////////////////////////////////////////////////////


BOOL APIENTRY DllMain(HMODULE hModule,
	DWORD  ul_reason_for_call,
	LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:				
		break;
	case DLL_THREAD_ATTACH:
		break;
	case DLL_THREAD_DETACH:
		break;
	case DLL_PROCESS_DETACH:			
		break;
	}
	return TRUE;
}

////////////////////////////////////////////////////////////////////////////////////////////////////


extern "C" DllExport uint16_t* SetFullGrayscaleImage(uint16_t* grayImage, uint16_t imageWidth, uint16_t imageHeight)
{
	if (imageWidth != m_imageW || imageHeight != m_imageH)
	{
		if (mp_d_grayImage != 0) cudaFree(mp_d_grayImage);
		if (mp_d_colorImage != 0) cudaFree(mp_d_colorImage);

		m_imageW = imageWidth;
		m_imageH = imageHeight;
		cudaError res = cudaMalloc(&mp_d_grayImage, m_imageW*m_imageH*sizeof(uint16_t));
		res = cudaMalloc(&mp_d_colorImage, m_imageW*m_imageH * 4);
	}
	
	cudaError_t err = cudaMemcpy(mp_d_grayImage, grayImage, m_imageW*m_imageH*sizeof(uint16_t), cudaMemcpyHostToDevice);

	return mp_d_grayImage;
}

extern "C" DllExport uint16_t* SetRoiGrayscaleImage(uint16_t* roiImage, uint16_t imageWidth, uint16_t imageHeight, uint16_t roiWidth, uint16_t roiHeight, uint16_t roiX, uint16_t roiY)
{
	if (imageWidth != m_imageW || imageHeight != m_imageH)
	{
		if (mp_d_grayImage != 0) cudaFree(mp_d_grayImage);
		if (mp_d_colorImage != 0) cudaFree(mp_d_colorImage);

		m_imageW = imageWidth;
		m_imageH = imageHeight;
		cudaError res = cudaMalloc(&mp_d_grayImage, m_imageW*m_imageH*sizeof(uint16_t));
		res = cudaMalloc(&mp_d_colorImage, m_imageW*m_imageH * 4);
	} 

	if (roiWidth != m_roiW || roiHeight != m_roiH || roiX != m_roiX || roiY != m_roiY)
	{
		if (mp_d_roiImage != 0) cudaFree(mp_d_roiImage);		

		m_roiW = roiWidth;
		m_roiH = roiHeight;		
		m_roiX = roiX;
		m_roiY = roiY;
		cudaMalloc(&mp_d_roiImage, m_roiW*m_roiH * sizeof(uint16_t));
	}		

	cudaError_t errNo = cudaMemcpy(mp_d_roiImage, roiImage, m_roiW*m_roiH*sizeof(uint16_t), cudaMemcpyHostToDevice);

	Call_CopyRoiToFullImage(mp_d_grayImage, mp_d_roiImage, m_imageW, m_imageH, m_roiX, m_roiY, m_roiW, m_roiH);

	return mp_d_grayImage;
}

extern "C" DllExport uint16_t* GetGrayscaleImagePtr()
{
	return mp_d_grayImage;
}

extern "C" DllExport uint16_t* SetMaskImage(uint16_t* maskImage, uint16_t maskWidth, uint16_t maskHeight, uint16_t maskRows, uint16_t maskCols)
{
	if (m_maskW != maskWidth || m_maskH != maskHeight)
	{
		if (mp_d_maskImage != 0) cudaFree(mp_d_maskImage);

		m_maskW = maskWidth;
		m_maskH = maskHeight;
		m_maskRows = maskRows;
		m_maskCols = maskCols;
		cudaMalloc(&mp_d_maskImage, m_maskW*m_maskH*sizeof(uint16_t));
	}


	// copy mask image to GPU
	cudaMemcpy(mp_d_maskImage, maskImage, m_maskW*m_maskH*sizeof(uint16_t), cudaMemcpyHostToDevice);

	m_maskSet = true;

	return mp_d_maskImage;
}

extern "C" DllExport uint16_t* GetMaskImagePtr()
{
	return mp_d_maskImage;
}

extern "C" DllExport void SetColorMap(uint8_t* redMap, uint8_t* greenMap, uint8_t* blueMap, uint16_t maxPixelValue)
{
	cudaError_t res;
	if (mp_d_redMap != 0) cudaFree(mp_d_redMap);
	if (mp_d_greenMap != 0) cudaFree(mp_d_greenMap);
	if (mp_d_blueMap != 0) cudaFree(mp_d_blueMap);

	res = cudaMalloc(&mp_d_redMap, maxPixelValue + 1);
	res = cudaMalloc(&mp_d_greenMap, maxPixelValue + 1);
	res = cudaMalloc(&mp_d_blueMap, maxPixelValue + 1);
	
	res = cudaMemcpy(mp_d_redMap, redMap, maxPixelValue + 1, cudaMemcpyHostToDevice);
	res = cudaMemcpy(mp_d_greenMap, greenMap, maxPixelValue + 1, cudaMemcpyHostToDevice);
	res = cudaMemcpy(mp_d_blueMap, blueMap, maxPixelValue + 1, cudaMemcpyHostToDevice);
	
	m_maxPixelValue = maxPixelValue;

	m_colorMapSet = true;
}

extern "C" DllExport uint8_t* ConvertGrayscaleToColor(uint16_t scaleLower, uint16_t scaleUpper)
{
	if (m_colorMapSet && mp_d_grayImage != 0)
	{		
		if (mp_d_colorImage == 0) cudaMalloc(&mp_d_colorImage, m_imageW*m_imageH*4);		

		Call_ConvertGrayscaleToColor(mp_d_colorImage, mp_d_grayImage, mp_d_redMap, mp_d_greenMap, mp_d_blueMap, m_imageW, m_imageH, m_maxPixelValue, scaleLower, scaleUpper);
	}

	return mp_d_colorImage;
}

extern "C" DllExport uint8_t* GetColorImagePtr()
{
	return mp_d_colorImage;
}

extern "C" DllExport void ApplyMaskToImage()
{
	if (m_maskSet)
	{
		Call_MaskImage(mp_d_grayImage, mp_d_maskImage, m_imageW, m_imageH);
	}
}

extern "C" DllExport uint8_t* PipelineFullImage(uint16_t* grayImage, uint16_t imageWidth, uint16_t imageHeight, bool applyMask)
{
	SetFullGrayscaleImage(grayImage, imageWidth, imageHeight);
	if (applyMask) ApplyMaskToImage();
	ConvertGrayscaleToColor(0,m_maxPixelValue);

	return mp_d_colorImage;
}

extern "C" DllExport uint8_t* PipelineRoiImage(uint16_t* roiImage, uint16_t imageWidth, uint16_t imageHeight, uint16_t roiWidth, uint16_t roiHeight, uint16_t roiX, uint16_t roiY, bool applyMask)
{
	SetRoiGrayscaleImage(roiImage, imageWidth, imageHeight, roiWidth, roiHeight, roiX, roiY);
	if (applyMask) ApplyMaskToImage();
	ConvertGrayscaleToColor(0,m_maxPixelValue);

	return mp_d_colorImage;
}

extern "C" DllExport void DownloadColorImage(uint8_t* colorImageDest)
{
	cudaMemcpy(colorImageDest, mp_d_colorImage, m_imageW*m_imageH*4, cudaMemcpyDeviceToHost);
}

extern "C" DllExport void DownloadGrayscaleImage(uint16_t* grayImageDest)
{
	cudaMemcpy(grayImageDest, mp_d_grayImage, m_imageW * m_imageH * sizeof(UINT16), cudaMemcpyDeviceToHost);
}

extern "C" DllExport void CudaInit()
{
	if (mp_cuda != 0)
	{
		mp_cuda->Init();
	}
}


extern "C" DllExport void Init()
{
	mp_cuda = new CudaUtil();
	
	mp_d_grayImage = 0;
	mp_d_colorImage = 0;
	mp_d_maskImage = 0;
	mp_d_roiImage = 0;
	mp_d_redMap = 0;
	mp_d_greenMap = 0;
	mp_d_blueMap = 0;
	m_colorMapSet = false;
	m_maskSet = false;
	m_imageW = 0;
	m_imageH = 0;
	m_roiW = 0;
	m_roiH = 0;
	m_roiX = 0;
	m_roiY = 0;
	m_maskW = 0;
	m_maskH = 0;
	m_maskRows = 0;
	m_maskCols = 0;
	m_maxPixelValue = 65535;
	mp_d_histogram = 0;
	mp_d_colorHistogramImage = 0;
	
	mp_d_FFC_Fluor_Gc = 0; 
	mp_d_FFC_Fluor_Dc = 0;
	mp_d_FFC_Lumi_Gc = 0;  
	mp_d_FFC_Lumi_Dc = 0;  
	m_h_FFC_numElements = 0;


	// not sure why I have to do this, bu
	cudaMalloc(&mp_d_grayImage, 10);
	cudaMalloc(&mp_d_colorImage, 10);


	
}

extern "C" DllExport void Shutdown()
{
	if (mp_d_grayImage != 0) {
		cudaError_t err = cudaFree(mp_d_grayImage);
		mp_d_grayImage = 0;
	}
	if (mp_d_colorImage != 0) {
		cudaFree(mp_d_colorImage);
		mp_d_colorImage = 0;
	}
	if (mp_d_maskImage != 0) {
		cudaFree(mp_d_maskImage);
		mp_d_maskImage = 0;
	}
	if (mp_d_roiImage != 0) {
		cudaFree(mp_d_roiImage);
		mp_d_roiImage = 0;
	}
	if (mp_d_redMap != 0) {
		cudaFree(mp_d_redMap);
		mp_d_redMap = 0;
	}
	if (mp_d_greenMap != 0) {
		cudaFree(mp_d_greenMap);
		mp_d_greenMap = 0;
	}
	if (mp_d_blueMap != 0) {
		cudaFree(mp_d_blueMap);
		mp_d_blueMap = 0;
	}
	if (mp_d_histogram != 0){
		cudaFree(mp_d_histogram);
		mp_d_histogram = 0;
	}
	if (mp_d_colorHistogramImage != 0){
		cudaFree(mp_d_colorHistogramImage);
		mp_d_colorHistogramImage = 0;
	}
	if (mp_d_maskApertureSums != 0){
		cudaFree(mp_d_maskApertureSums);
		mp_d_maskApertureSums = 0;
	}	
	if (mp_d_FFC_Fluor_Gc != 0){
		cudaFree(mp_d_FFC_Fluor_Gc);
		mp_d_FFC_Fluor_Gc = 0;
	}
	if (mp_d_FFC_Fluor_Dc != 0){
		cudaFree(mp_d_FFC_Fluor_Dc);
		mp_d_FFC_Fluor_Dc = 0;
	}
	if (mp_d_FFC_Lumi_Gc != 0){
		cudaFree(mp_d_FFC_Lumi_Gc);
		mp_d_FFC_Lumi_Gc = 0;
	}
	if (mp_d_FFC_Lumi_Dc != 0){
		cudaFree(mp_d_FFC_Lumi_Dc);
		mp_d_FFC_Lumi_Dc = 0;
	}
	
	delete mp_cuda;

	cudaDeviceReset();
}



extern "C" DllExport void GetHistogram_512Buckets(uint32_t* destHist, uint8_t maxValueBitWidth)
{	
	if (mp_d_histogram == 0)
	{
		cudaMalloc(&mp_d_histogram, 512 * sizeof(uint32_t));
	}

	cudaMemset(mp_d_histogram, 0, 512 * sizeof(uint32_t));
	
	Call_ComputeHistogram_512(mp_d_histogram, mp_d_grayImage, m_imageW, m_imageH, maxValueBitWidth);
	
	//cudaMemset(mp_d_histogram, 0, sizeof(uint32_t));  // zero the first bin, since that is the pixels that were masked out

	cudaMemcpy(destHist, mp_d_histogram, 512 * sizeof(uint32_t), cudaMemcpyDeviceToHost);

	m_max_histogramBinValue = 0;

	for (int i = 1; i < 512; i++)
	{
		if (destHist[i] > m_max_histogramBinValue) m_max_histogramBinValue = destHist[i];
	}
	
}

extern "C" DllExport void GetHistogramImage_512Buckets(uint8_t* histImage, uint16_t width, uint16_t height, uint32_t maxBinCount)
{
	// NOTE:  GetHistogram_512Buckets MUST BE CALLED BEFORE CALLING THIS FUNCTION!!

	if (mp_d_colorHistogramImage == 0)
	{
		cudaMalloc(&mp_d_colorHistogramImage, width*height * 4);
	}

	if (maxBinCount == 0) maxBinCount = m_max_histogramBinValue;

	Call_BuildHistogramImage_512(mp_d_colorHistogramImage, mp_d_histogram, 512, width, height, maxBinCount);

	cudaMemcpy(histImage, mp_d_colorHistogramImage, width * height * 4, cudaMemcpyDeviceToHost);
}


extern "C" DllExport void CalculateMaskApertureSums(uint32_t* sums)
{
	if (mp_d_maskApertureSums != 0)	cudaFree(mp_d_maskApertureSums);
	uint32_t numApertures = m_maskRows * m_maskCols;
	cudaMalloc(&mp_d_maskApertureSums, numApertures * sizeof(uint32_t));
	cudaMemset(mp_d_maskApertureSums, 0, numApertures * sizeof(uint32_t));

	Call_CalcApertureSums(mp_d_maskApertureSums, mp_d_grayImage, mp_d_maskImage, m_imageW, m_imageH);

	cudaMemcpy(sums, mp_d_maskApertureSums, numApertures * sizeof(uint32_t), cudaMemcpyDeviceToHost);
}


extern "C" DllExport void SetFlatFieldCorrectionArrays(int type, float* Gc, float* Dc, int numElements)
{	
	// 1 = Fluor
	// 2 = Lumi

	if (type < 1 || type > 2) type = 1;	

	m_h_FFC_numElements = (uint32_t)numElements;

	switch (type)
	{
	case 1:
		if (mp_d_FFC_Fluor_Gc != 0)
		{
			cudaError_t err = cudaFree(mp_d_FFC_Fluor_Gc);
		}
		if (mp_d_FFC_Fluor_Dc != 0)
		{
			cudaError_t err = cudaFree(mp_d_FFC_Fluor_Dc);
		}

		cudaMalloc(&mp_d_FFC_Fluor_Gc, numElements * sizeof(float));
		cudaMalloc(&mp_d_FFC_Fluor_Dc, numElements * sizeof(float));

		cudaMemcpy(mp_d_FFC_Fluor_Gc, Gc, numElements*sizeof(float), cudaMemcpyHostToDevice);
		cudaMemcpy(mp_d_FFC_Fluor_Dc, Dc, numElements*sizeof(float), cudaMemcpyHostToDevice);
		break;
	case 2:
		if (mp_d_FFC_Lumi_Gc != 0)
		{
			cudaError_t err = cudaFree(mp_d_FFC_Lumi_Gc);
		}
		if (mp_d_FFC_Lumi_Dc != 0)
		{
			cudaError_t err = cudaFree(mp_d_FFC_Lumi_Dc);
		}

		cudaMalloc(&mp_d_FFC_Lumi_Gc, numElements * sizeof(float));
		cudaMalloc(&mp_d_FFC_Lumi_Dc, numElements * sizeof(float));

		cudaMemcpy(mp_d_FFC_Lumi_Gc, Gc, numElements*sizeof(float), cudaMemcpyHostToDevice);
		cudaMemcpy(mp_d_FFC_Lumi_Dc, Dc, numElements*sizeof(float), cudaMemcpyHostToDevice);
		break;
	}

}


extern "C" DllExport void FlattenImage(int type)
{
	if (mp_d_grayImage == 0) return; // no image to flatten (a call to SetFullGrayscaleImage or SetRoiGrayscaleImage has not been made)
	
	// make sure that the flat field corrector is initialized, if not initialize it so that it has no effect on images
	if (m_h_FFC_numElements != (m_imageW*m_imageH))
	{
		m_h_FFC_numElements = m_imageW*m_imageH;
		float* gc = (float*)malloc(m_imageW*m_imageH*sizeof(float));
		float* dc = (float*)malloc(m_imageW*m_imageH*sizeof(float));
		for (int i = 0; i < m_h_FFC_numElements; i++)
		{
			gc[i] = 1.0;
			dc[i] = 0.0;
		}
		SetFlatFieldCorrectionArrays(1, gc, dc, m_h_FFC_numElements);
		SetFlatFieldCorrectionArrays(2, gc, dc, m_h_FFC_numElements);
	}

	switch (type)
	{
	case 0: // no flattening
		break;
	case 1: // Fluor flattening
		Call_FlattenImage(mp_d_grayImage, mp_d_FFC_Fluor_Gc, mp_d_FFC_Fluor_Dc, m_imageW, m_imageH);
		break;
	case 2: // Lumi flattening
		Call_FlattenImage(mp_d_grayImage, mp_d_FFC_Lumi_Gc, mp_d_FFC_Lumi_Dc, m_imageW, m_imageH);
		break;
	}
}