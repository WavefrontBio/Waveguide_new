
#include "cuda_runtime.h"
#include "device_launch_parameters.h"

#include <stdio.h>
#include <stdint.h>

#include <iostream>
#include <cuda.h>
#include <device_launch_parameters.h>
#include <cuda_runtime.h>
#include <device_functions.h>


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Utility Functions
struct GpuTimer
{
	cudaEvent_t start;
	cudaEvent_t stop;

	GpuTimer()
	{
		cudaEventCreate(&start);
		cudaEventCreate(&stop);
	}

	~GpuTimer()
	{
		cudaEventDestroy(start);
		cudaEventDestroy(stop);
	}

	void Start()
	{
		cudaEventRecord(start, 0);
	}

	void Stop()
	{
		cudaEventRecord(stop, 0);
	}

	float ElapsedMillis()
	{
		float elapsed;
		cudaEventSynchronize(stop);
		cudaEventElapsedTime(&elapsed, start, stop);
		return elapsed;
	}
};


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Cuda Kernels

__global__ void Compute_Histogram_512_Cuda(uint32_t* hist, const uint16_t* data, uint16_t width, uint16_t height, uint8_t maxValueBitWidth)
{
	// NOTE: # of bins of histogram must match block size (number of threads in block), and in this case must be 512.
	//		 i.e. the number of threads per block must be the same as the number of bins.

	// maxValueBitWidth = the number of bits needed to represent the max value in the data array.  For example, if the data
	//					  array is built from a 10-bit A-to-D converter, then maxValueBitWidth = 10 since no value will be greather 
	//					  than 2^10.  The minimum value for maxValueBitWidth is driven by the number of bins.  For 256 bins (2^8), 
	//					  the min value is 8.  If bins were 1024 (i.e. 2^10), then the min value for maxValueBitWidth would be 10.

	if (maxValueBitWidth < 8) maxValueBitWidth = 8; // make sure we aren't below the min as described above

	int x = blockIdx.x * blockDim.x + threadIdx.x;
	int y = blockIdx.y * blockDim.y + threadIdx.y;
	int nThread = threadIdx.y * blockDim.x + threadIdx.x; // index of thread within block
	int nPixel = y * width + x; // index of pixel within image

	if (x >= width) return;
	if (y >= height) return;

	// if image pixel value == 0, don't add it to the histogram.  Pixel that are 0 are pixels that are outside of the mask 
	// and thus should not be part of the histogram
	if (data[nPixel] == 0) return;

	//Create shared buffer size of threads per block and clear it 
	//Size of array equals numBins 
	__shared__ uint32_t tmpHist[512];
	tmpHist[nThread] = 0;
	__syncthreads();


	//based on the value of this pixel, find the correct bin of the local histogram to increment, and then increment it
	uint8_t shift = maxValueBitWidth - 9;
	int binNumber = data[nPixel] >> shift;

	if (binNumber>511)
	{
		binNumber = 511;
	}

	//float f1 = ((float)(data[nPixel]))/1023.0 * 255;
	//uint8_t binNumber = (uint8_t)f1;


	atomicAdd(&(tmpHist[binNumber]), 1);
	__syncthreads();  // wait for all threads in this block to finish so that the local histogram is finished

	// Update global memory (global histogram)	
	atomicAdd(&(hist[nThread]), tmpHist[nThread]);

}

__global__ void compute_histogram_256_Cuda(uint32_t* hist, const uint16_t* data, uint16_t width, uint16_t height, uint8_t maxValueBitWidth)
{
	// NOTE: # of bins of histogram must match block size (number of threads in block), and in this case must be 256.
	//		 i.e. the number of threads per block must be the same as the number of bins.

	// maxValueBitWidth = the number of bits needed to represent the max value in the data array.  For example, if the data
	//					  array is built from a 10-bit A-to-D converter, then maxValueBitWidth = 10 since no value will be greather 
	//					  than 2^10.  The minimum value for maxValueBitWidth is driven by the number of bins.  For 256 bins (2^8), 
	//					  the min value is 8.  If bins were 1024 (i.e. 2^10), then the min value for maxValueBitWidth would be 10.

	if (maxValueBitWidth < 8) maxValueBitWidth = 8; // make sure we aren't below the min as described above

	int x = blockIdx.x * blockDim.x + threadIdx.x;
	int y = blockIdx.y * blockDim.y + threadIdx.y;
	int nThread = threadIdx.y * blockDim.x + threadIdx.x; // index of thread within block
	int nPixel = y * width + x; // index of pixel within image

	if (x >= width) return;
	if (y >= height) return;

	//Create shared buffer size of threads per block and clear it 
	//Size of array equals numBins 
	__shared__ uint32_t tmpHist[256];
	tmpHist[nThread] = 0;
	__syncthreads();


	//based on the value of this pixel, find the correct bin of the local histogram to increment, and then increment it
	uint8_t shift = maxValueBitWidth - 8;
	int binNumber = data[nPixel] >> shift;

	if (binNumber>255)
	{
		binNumber = 255;
	}

	//float f1 = ((float)(data[nPixel]))/1023.0 * 255;
	//uint8_t binNumber = (uint8_t)f1;


	atomicAdd(&(tmpHist[binNumber]), 1);
	__syncthreads();  // wait for all threads in this block to finish so that the local histogram is finished

	// Update global memory (global histogram)	
	atomicAdd(&(hist[nThread]), tmpHist[nThread]);

}

__global__ void MaskImage_Cuda(uint16_t* image, uint16_t* mask, uint16_t width, uint16_t height, float* flatFieldCorrectionArray)
{
	// this function zeroes out all pixels in image that are not in the mask

	// image - a greyscale image with each pixel being a uint16_t
	// mask - a image where pixels with value>0 will be passed through, and pixels with value==0 will be masked out (set to zero).
	//		  The mask is created where pixels with a value of 1, belong in mask aperture 1.  Pixels with value of 2, belong in 
	//		  mask aperture 2...and so on.  
	// width,height - dimensions of image in pixels

	// calc x,y position of pixel to operate on
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside panel
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside panel

	// make sure we don't try to operate outside the image
	if (x >= width) return;
	if (y >= height) return;

	// calculate pixel position in array
	uint32_t n = (y * width) + x;

	// apply mask to image
	if (mask[n] == 0)
	{	
		// this pixel is not within a mask aperture, so zero it out
		image[n] = 0;  
	}
	else
	{
		// this pixel is within a mask aperture.  Read the mask aperture value and use that value as an index into the 
		// flat field correction array

		float ffcGain = flatFieldCorrectionArray[mask[n]-1]; // subtract 1 since the mask values are 1-based, and the ffc array is 0-based.
		image[n] = (uint32_t)(ffcGain * ((float)image[n]));
	}
}

__global__ void ConvertGrayscaleToColor_Cuda(uint8_t* color, uint16_t* gray, uint8_t* redMap, uint8_t* greenMap, uint8_t* blueMap,
	uint16_t width, uint16_t height, uint16_t maxGrayValue, uint16_t scaleLower, uint16_t scaleUpper)
{
	// this function converts a grayscale image to a color image using the provided color map

	// color - destination color image (format is ARGB)
	// gray -  source grayscale image
	// redMap, greenMap, blueMap - arrays (maps) that provide color components for each possible grayscale value. For example,
	//							   if a pixel in the gray image has a value = 100, then the corresponding pixel in the color image
	//							   would have its RGB component values set to redMap[100], greenMap[100], and blueMap[100], respectively.
	// width, height - image dimensions
	// maxGrayValue - the maximum possible grayscale value, i.e. length of color map (length of redMap, greenMap, and blueMap)

	// scaleLower, scaleUpper - these values are used to scale the grayscale value of a pixel before it is converted to color.
	//
	//                         scaleUpper
	//						   ________________
	//	maxGrayValue|         /
	//				|        /
	//				|       /
	//				|      /
	//			0	|_____/____________________ 
	//                   scaleLower
	//
	//  Here's the math:
	//		if (pixelValue < scaleLower) set pixelValue = 0
	//      else if (pixelValue < scaleUpper) set pixelValue = maxGrayValue
	//      else 

	// calc x,y position of pixel to operate on
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside panel
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside panel

	// make sure we don't try to operate outside the image
	if (x >= width) return;
	if (y >= height) return;

	// calculate pixel position in gray array
	uint32_t nG = (y * width) + x;

	// calculate pixel position in color array
	uint32_t nC = (y * width * 4) + (x * 4);

	// make sure grayscale value is not outside of color maps
	if (gray[nG] > maxGrayValue) gray[nG] = maxGrayValue;

	// scale the value
	uint16_t val = gray[nG];
	if (val < scaleLower) val = 0;
	else if (val >= scaleUpper) val = maxGrayValue;
	else 
		{
			float fval = (float)maxGrayValue/(float)(scaleUpper-scaleLower) * (float)(val-scaleLower);
			val = (uint16_t)fval;
		}

	// set pixel component values for color image
	color[nC + 0] = blueMap[val];	// blue
	color[nC + 1] = greenMap[val];	// green
	color[nC + 2] = redMap[val];	// red
	color[nC + 3] = 255;			// alpha

}

__global__ void CopyCudaArrayToD3D9Memory_Cuda(uint8_t *dest, uint8_t *source, uint16_t pitch, uint16_t width, uint16_t height)
{
	// calc x,y position of pixel to operate on
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside panel
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside panel

	// make sure we don't try to operate outside the image
	if (x >= width) return;
	if (y >= height) return;

	// calc position of pixel in cuda array (remember that pitch may not equal width)
	//uint32_t nD = ((height - 1 - y)*pitch) + (x * 4);
	uint32_t nD = (y*pitch) + (x * 4);
	uint32_t nS = (y*width * 4) + (x * 4);

	// copy data
	dest[nD] = source[nS];
	dest[nD + 1] = source[nS + 1];
	dest[nD + 2] = source[nS + 2];
	dest[nD + 3] = source[nS + 3];
}

__global__ void BuildHistogramImage_Cuda(uint8_t* histImage, uint32_t* hist, uint16_t numBins, uint16_t width, uint16_t height, uint32_t maxBinCount)
{
	// this function builds the image for a histogram given by the variable hist.  
	//
	// histImage - the output histogram image.  This is a color image (ARGB, 8 bits per component)
	// hist - is an array which contains the data for the histogram
	// numBins - is the number of bins in the histogram
	// width, height - dimensions of the histImage in pixels
	// maxBinCount - the maximum value that can appear in each bin of the histogram

	// calc x,y position of pixel to operate on
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside panel
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside panel

	// make sure we don't try to operate outside the image
	if (x >= width) return;
	if (y >= height) return;

	// calculate the array index into the histogram image
	uint32_t n = (y * width * 4) + (x * 4);  // ARGB image

	// calculate the width of each bin in pixels
	uint16_t binWidth = width / numBins;

	// calculate the bin that this pixel belongs in
	uint16_t binNumber = x / binWidth;
	if (binNumber>numBins) binNumber = numBins;

	// calculate height of the bar for his bin
	uint32_t value = hist[binNumber];  // get the height of the bar for this bin
	uint32_t barHeight = (uint32_t)((float)value * (float)height / (float)maxBinCount);  // calculate the bar height in pixels
	if (barHeight > height) barHeight = height; // make sure the bar height in pixels is not greater than the histogram image height

	// determine if this pixel is in the bar or above it (i.e. determine color of pixel)
	if (y < (height - barHeight)) // pixel is above bar (thus pixel is background color...likely white)
	{   
		histImage[n + 0] = 220;	// blue
		histImage[n + 1] = 220;	// green
		histImage[n + 2] = 220;	// red
		histImage[n + 3] = 255;	// alpha
	}
	else  // pixel is part of bar, so make it the color of the bar (likely black)
	{
		histImage[n + 0] = 0;	// blue
		histImage[n + 1] = 0;	// green
		histImage[n + 2] = 0;	// red
		histImage[n + 3] = 255;	// alpha
	}
}

__global__ void CalcApertureSums_Cuda(uint32_t* sumArray, uint16_t* image, uint16_t* mask, uint16_t width, uint16_t height)
{
	// This function calculate the sum of pixels for each aperture of a mask.  It expects that the mask is formated as follows:
	//		mask pixels with a value of 0 belong to no apertures, thus they will not be part of any sum
	//      mask pixels with a value of 1 belong in aperture 1, which is added to the value in sumArray[0]
	//      mask pixels with a value of 2 belong in aperture 2, which is added to the value in sumArray[1]
	//		and so on...

	// sumArray - output array of the sum of pixel values for each aperature.  For example, for a mask with 24x16 (384) apertures, there
	//			  will be 384 values in sumArray
	// image - input grayscale image from which sums are calculated
	// mask  - input mask that is formatted as described in the description above for this function
	// width, height - dimensions of the image and mask in pixels

	// calc x,y position of pixel to operate on
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside panel
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside panel

	// make sure we don't try to operate outside the image
	if (x >= width) return;
	if (y >= height) return;

	// calculate pixel position in image and mask
	uint32_t n = (y * width) + x;

	// get aperture number from mask
	if (mask[n] > 0) // is this pixel inside of any of the apertures of the mask?
	{ // yes
		atomicAdd(&sumArray[mask[n] - 1], image[n]);
	}

	__syncthreads();  // wait for all threads in this block to finish so that the local histogram is finished

}

__global__ void FlatField_Cuda(uint16_t* image, uint16_t* dark, uint16_t* gain, uint16_t width, uint16_t height)
{
	// this function flat field corrects the given grayscale image. It uses the following function:
	//
	//		C[i,j] = ((R[i,j] - D[i,j]) * m) / (F[i,j] - D[i,j]) = (R[i,j] - D[i,j]) * G[i,j]
	//
	//			where G[i,j] = m / (F[i,j] - D[i,j])
	//
	//				  m = average of F-D
	//
	//		i,j = row,column of pixel in image
	//		C = corrected image
	//		R = raw image
	//		F = flat field reference image (evenly illuminated image, meant to show unevenness of illumination)
	//		D = dark field reference image (image taken with no illumination, meant to show distribution of background)
	//		G = gain

	//	parameters passed into function:
	//	image - grayscale image to be corrected.  This is both the input and output image (the input image is over written)
	//  dark  - this is the dark field image (must be same dimensions as image), probably stored in database
	//  gain  - this is the gain array (must be same dimensions as image), that is calculated elsewhere
	//  width, height - dimensions of image (and dark) in pixels

	// calc x,y position of pixel to operate on
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside panel
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside panel

	// make sure we don't try to operate outside the image
	if (x >= width) return;
	if (y >= height) return;

	// calculate pixel position in image and dark arrays
	uint32_t n = (y * width) + x;

	image[n] = (image[n] - dark[n]) * gain[n];
}

__global__ void CopyRoiToFullImage_Cuda(uint16_t* full, uint16_t* roi, uint16_t fullW, uint16_t fullH,
	uint16_t  roiX, uint16_t roiY, uint16_t roiW, uint16_t roiH)
{
	// This function is used to copy a ROI image from the camera into a memory space that holds a full frame.
	// It is used when the camera is set up to capture only a part of the CCD (an Region of Interest - ROI), and 
	// since all of the algorithms, kernels, display routines, etc. are set up to handle full frames, this
	// function simply copies the ROI into a full frame.  Pixels outside the ROI are set to zero.

	// calc x,y position of pixel to operate on in the full frame
	uint32_t x = blockIdx.x * blockDim.x + threadIdx.x; // column of pixel inside full frame image
	uint32_t y = blockIdx.y * blockDim.y + threadIdx.y; // row of pixel inside full frame image

	// make sure we don't try to operate outside the full image
	if (x >= fullW) return;
	if (y >= fullH) return;

	// calculate pixel position in arrays
	uint32_t fullN = (y * fullW) + x;  // index into full frame

	// calculate x,y position in ROI
	int32_t xr = x - roiX;
	int32_t yr = y - roiY;

	// are we inside ROI?

	if (x >= roiX && x < (roiX + roiW) && y >= roiY && y < (roiY + roiH))
	{
		uint32_t roiN = (yr * roiW) + xr; // index into roi frame

		// inside ROI
		full[fullN] = roi[roiN];
	}
	else
	{
		// outside ROI
		full[fullN] = 0;
	}
}


/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Kernel Calling Functions

void Call_ConvertGrayscaleToColor(uint8_t* color, uint16_t* gray, uint8_t* redMap, uint8_t* greenMap, uint8_t* blueMap,
	uint16_t width, uint16_t height, uint16_t maxGrayValue, uint16_t scaleLower, uint16_t scaleUpper)
{
	dim3 block, grid;
	block.x = 32; block.y = 8; block.z = 1;
	grid.x = width / block.x;
	grid.y = height / block.y;
	grid.z = 1;

	ConvertGrayscaleToColor_Cuda << <grid, block >> >(color, gray, redMap, greenMap, blueMap, width, height, maxGrayValue, scaleLower, scaleUpper);

}

void Call_CopyRoiToFullImage(uint16_t* full, uint16_t* roi, uint16_t fullW, uint16_t fullH,
	uint16_t  roiX, uint16_t roiY, uint16_t roiW, uint16_t roiH)
{
	dim3 block, grid;
	block.x = 32; block.y = 8; block.z = 1;
	grid.x = fullW / block.x;
	grid.y = fullH / block.y;
	grid.z = 1;
	CopyRoiToFullImage_Cuda<<<grid,block>>>(full, roi, fullW, fullH, roiX, roiY, roiW, roiH);
}

void Call_MaskImage(uint16_t* image, uint16_t* mask, uint16_t width, uint16_t height, float* ffcArray)
{
	dim3 block, grid;
	block.x = 32; block.y = 8; block.z = 1;
	grid.x = width / block.x;
	grid.y = height / block.y;
	grid.z = 1;
	MaskImage_Cuda<<<grid,block>>>(image, mask, width, height, ffcArray);
}

void Call_CopyCudaArrayToD3D9Memory(uint8_t* pDest, uint8_t* pSource, uint16_t pitch, uint16_t width, uint16_t height)
{
	cudaError_t res = cudaDeviceSynchronize();

	dim3 threadsPerBlock(32, 32);  // 32x16 = 512 threads per block	
	dim3 numBlocks((width + threadsPerBlock.x - 1) / threadsPerBlock.x, (height + threadsPerBlock.y - 1) / threadsPerBlock.y);
 	CopyCudaArrayToD3D9Memory_Cuda << <numBlocks, threadsPerBlock >> >(pDest, pSource, pitch, width, height);
}

void Call_ComputeHistogram_512(uint32_t* hist, const uint16_t* data, uint16_t width, uint16_t height, uint8_t maxValueBitWidth)
{
	dim3 block, grid;
	block.x = 32; block.y = 16; block.z = 1; // block size must be 512 = 32 * 16
	grid.x = width / block.x;
	grid.y = height / block.y;
	grid.z = 1;

	Compute_Histogram_512_Cuda<<<grid,block>>>(hist, data, width, height, maxValueBitWidth);
}

void Call_BuildHistogramImage_512(uint8_t* histImage, uint32_t* hist, uint16_t numBins, uint16_t width, uint16_t height, uint32_t maxBinCount)
{
	dim3 block, grid;
	block.x = 32; block.y = 16; block.z = 1;
	grid.x = width / block.x;
	grid.y = height / block.y;
	grid.z = 1;

	BuildHistogramImage_Cuda << <grid, block >> >(histImage, hist, numBins, width, height, maxBinCount);
}

void Call_CalcApertureSums(uint32_t* sumArray, uint16_t* image, uint16_t* mask, uint16_t width, uint16_t height)
{
	dim3 block, grid;
	block.x = 32; block.y = 16; block.z = 1;
	grid.x = width / block.x;
	grid.y = height / block.y;
	grid.z = 1;

	CalcApertureSums_Cuda << <grid, block >> >(sumArray, image, mask, width, height);
}
