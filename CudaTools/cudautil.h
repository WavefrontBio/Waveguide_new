#ifndef CUDAUTIL_H
#define CUDAUTIL_H

#include <string>
#include <map>
#include <stdint.h>
#include <cuda.h>
#include "cuda_runtime.h"

#include "diagnostics.h"




class CudaUtil
{
public:
	CudaUtil();    
    ~CudaUtil();

	void Init();
    std::string GetCudaErrorMessage(CUresult cudaResult);
    static std::string GetCudaErrorDescription(CUresult result);
    bool GetCudaDeviceCount(int &count);
    bool GetComputeCapability(int &major, int &minor);
    bool GetDeviceName(std::string &name);
    bool GetDeviceMemory(size_t &totalMem, size_t &freeMem);
    bool GetContext(CUcontext **pCtx);
    bool IsCudaReady();
    std::string GetLastErrorMessage();

	

private:
    CUresult m_result;
    CUcontext          m_cudaContext;
    CUdevice           m_cudaDevice;
    std::string m_errMsg;
    bool m_cudaDriverReady;
    int  m_deviceCount;	
};

#endif // CUDAUTIL_H
