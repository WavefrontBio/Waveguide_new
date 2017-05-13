#pragma once

#include <map>
#include <string>
#include "stdafx.h"


struct SURFACE_DATA {
	UINT		Width;
	UINT		Height;
	bool		UseAlpha;	
	bool		SurfaceSettingsChanged;
	HWND		hwnd;
	//IDirect3DDevice9   *pDevice;
	//IDirect3DDevice9Ex *pDeviceEx;
	IDirect3DSurface9  *pSurface;
public:
	SURFACE_DATA(){
		Width = 0; Height = 0; UseAlpha = true;
		SurfaceSettingsChanged = false; hwnd = NULL; /*pDevice = NULL; pDeviceEx = NULL; */pSurface = NULL;
	}
};




class CSurfaceManager
{	
public:
	static HRESULT Create(CSurfaceManager **ppManager);
	static void Release(CSurfaceManager **ppManager);
	~CSurfaceManager();

	HRESULT CreateNewSurface(UINT uWidth, UINT uHeight, bool UseAlpha, int &index);
	bool	DestroySurface(int SurfaceIndex);
	HRESULT LoadNewImage(int SurfaceIndex, UINT8* pImageData, UINT width, UINT height, UINT numBytes);
	HRESULT LoadNewGPUImage(int SurfaceIndex, UINT8* pImageData, UINT width, UINT height, UINT numBytes);
	HRESULT	GetBackBufferNoRef(int SurfaceIndex, IDirect3DSurface9 **ppSurface);

	void	GetD3D_Objects(IDirect3D9Ex** ppD3DEx, IDirect3DDevice9** ppDevice, IDirect3DDevice9Ex ** ppDeviceEx);
	bool    GetSurfaceParams(int SurfaceIndex, IDirect3DSurface9** ppSurface, int* pWidth, int* pHeight, bool* pUseAlpha);
	
private:
	CSurfaceManager();

	HRESULT EnsureD3DObjects();
	HRESULT EnsureHWND(int SurfaceIndex);
	HRESULT CreateDevice(IDirect3DDevice9 **pd3dDevice, IDirect3DDevice9Ex **pd3dDeviceEx, HWND hwnd);
	HRESULT CheckDeviceState();
	
	void	CleanupInvalidDevices();

	HRESULT	GetSurfaceData(int SurfaceIndex, char **pImageData, int *pWidth, int *pHeight);

	IDirect3D9    *m_pD3D;
	IDirect3D9Ex  *m_pD3DEx;

	IDirect3DDevice9   * m_pDevice;
	IDirect3DDevice9Ex * m_pDeviceEx;

	std::map<int, SURFACE_DATA*> m_SurfaceMap;

	int m_nextAvailableIndex;
};