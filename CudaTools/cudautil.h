#ifndef CUDAUTIL_H
#define CUDAUTIL_H

#include <string>
#include <map>
#include <stdint.h>
#include "cuviddec.h"
#include "cuda_runtime.h"
#include <d3dx9.h>
#include "cudaD3D9.h"
#include <cuda_d3d9_interop.h>
#include "diagnostics.h"


/////////////////////////////////////////////////////////
// Forward Declaration

void Call_CopyCudaArrayToD3D9Memory(uint8_t* pDest, uint8_t* pSource, uint16_t pitch, uint16_t width, uint16_t height);

/////////////////////////////////////////////////////////


struct D3D9Params
{
	D3D9Params(){ pSurface = 0; cudaResource = 0; cudaLinearMemory = 0; }
	IDirect3DSurface9      *pSurface;
	cudaGraphicsResource   *cudaResource;
	void				   *cudaLinearMemory;
	size_t					pitch;
	int						width;
	int						height;
};




class CudaUtil
{
public:
	CudaUtil();    
    ~CudaUtil();

	void Init(IDirect3DDevice9Ex* pDeviceEx);
    std::string GetCudaErrorMessage(CUresult cudaResult);
    static std::string GetCudaErrorDescription(CUresult result);
    bool GetCudaDeviceCount(int &count);
    bool GetComputeCapability(int &major, int &minor);
    bool GetDeviceName(std::string &name);
    bool GetDeviceMemory(size_t &totalMem, size_t &freeMem);
    bool GetContext(CUcontext **pCtx);
    bool IsCudaReady();
    std::string GetLastErrorMessage();




	// DirectX Stuff	
	//bool RegisterD3D9ResourceWithCUDA(D3D9Params *pD3D9);
	bool CopyImageToSurface(int SurfaceIndex, CUdeviceptr ImageData);
	bool RemoveD3DSurface(int SurfaceIndex);
	bool AddD3DSurface(int SurfaceIndex, IDirect3DSurface9 *pSurface, int width, int height);
	

private:
    CUresult m_result;
    CUcontext          m_cudaContext;
    CUdevice           m_cudaDevice;
    std::string m_errMsg;
    bool m_cudaDriverReady;
    int  m_deviceCount;

	std::map<int, D3D9Params*> m_SurfaceMap;
	IDirect3DDevice9Ex* mp_d3d9DeviceEx;
};

#endif // CUDAUTIL_H
