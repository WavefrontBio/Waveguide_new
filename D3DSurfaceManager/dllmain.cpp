// dllmain.cpp : Defines the entry point for the DLL application.
#include "stdafx.h"

#define DllExport   __declspec( dllexport ) 


static CSurfaceManager *pManager = NULL;
static bool Ready = false;


HRESULT EnsureSurfaceManager()
{
	return pManager ? S_OK : CSurfaceManager::Create(&pManager);
}



BOOL APIENTRY DllMain( HMODULE hModule,
                       DWORD  ul_reason_for_call,
                       LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}


extern "C" DllExport int Test()
{
	return 99;
}

extern "C" DllExport void CreateSurfaceManager()
{
	if (FAILED(EnsureSurfaceManager()))
		Ready = false;
	else
		Ready = true;	
}

extern "C" DllExport void ReleaseSurfaceManager()
{
	if (pManager)
	{
		CSurfaceManager::Release(&pManager);
		pManager = NULL;
		Ready = false;
	}
}

extern "C" DllExport int CreateNewSurface(UINT uWidth, UINT uHeight, bool UseAlpha)
{
	if (!Ready) return -2; // CreateSurfaceManager needs to be called first

	HRESULT hr = S_OK;
	int index = -3;

	hr = pManager->CreateNewSurface(uWidth, uHeight, UseAlpha, index);

	if (FAILED(hr))
		index = -1;  // failed to create new surface

	return index;
}

extern "C" DllExport bool DestroySurface(int SurfaceIndex)
{
	if (!Ready) return false;

	bool success = false;

	if (pManager)
		success = pManager->DestroySurface(SurfaceIndex);

	return success;
}


extern "C" DllExport int LoadNewImage(int SurfaceIndex, UINT8* pImageData, UINT width, UINT height, UINT numBytes)
{
	if (!Ready) return -2;  // CreateSurfaceManager needs to be called first

	HRESULT hr = pManager->LoadNewImage(SurfaceIndex, pImageData, width, height, numBytes);
	
	return (int)hr;
}


extern "C" DllExport int GetBackBufferNoRef(int SurfaceIndex, IDirect3DSurface9 **ppSurface)
{
	if (!Ready) return -2;  // CreateSurfaceManager needs to be called first

	HRESULT hr = pManager->GetBackBufferNoRef(SurfaceIndex, ppSurface);

	if (FAILED(hr))
		hr = -1;  // failed to get back buffer
	else
		hr = 1;  // success

	return (int)hr;
}

extern "C" DllExport void GetD3D_Objects(IDirect3D9Ex **ppD3D, IDirect3DDevice9 **ppDevice, IDirect3DDevice9Ex **ppDeviceEx)
{
	pManager->GetD3D_Objects(ppD3D, ppDevice, ppDeviceEx);
}


extern "C" DllExport void GetD3D_SurfaceParams(int SurfaceIndex, IDirect3DSurface9 **ppSurface, int *pWidth, int * pHeight, bool *pUseAlpha)
{
	pManager->GetSurfaceParams(SurfaceIndex, ppSurface, pWidth, pHeight, pUseAlpha);
}