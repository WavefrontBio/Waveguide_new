// D3DSurfaceManager.cpp : Defines the exported functions for the DLL application.
//
#include "stdafx.h"
#include "D3DSurfaceManager.h"

const static TCHAR szAppName[] = TEXT("D3DSurfaceManager");
typedef HRESULT(WINAPI *DIRECT3DCREATE9EXFUNCTION)(UINT SDKVersion, IDirect3D9Ex**);

CSurfaceManager::CSurfaceManager()
{
	m_nextAvailableIndex = 0;
}

CSurfaceManager::~CSurfaceManager()
{
	SAFE_RELEASE(m_pDevice);
	SAFE_RELEASE(m_pDeviceEx);
}

HRESULT CSurfaceManager::Create(CSurfaceManager **ppManager)
{
	HRESULT hr = S_OK;

	*ppManager = new CSurfaceManager();

	IFCOOM(*ppManager);


Cleanup:
	return hr;
}


void CSurfaceManager::Release(CSurfaceManager **ppManager)
{
	delete *ppManager;
}


//+-----------------------------------------------------------------------------
//
//  Member:
//      CSurfaceManager::EnsureD3DObjects
//
//  Synopsis:
//      Makes sure the D3D objects exist
//
//------------------------------------------------------------------------------
HRESULT
CSurfaceManager::EnsureD3DObjects()
{
	HRESULT hr = S_OK;

	HMODULE hD3D = NULL;
	
	if (!m_pD3DEx)
	{
		hD3D = LoadLibrary(TEXT("d3d9.dll"));
		DIRECT3DCREATE9EXFUNCTION pfnCreate9Ex = (DIRECT3DCREATE9EXFUNCTION)GetProcAddress(hD3D, "Direct3DCreate9Ex");
		if (pfnCreate9Ex)
		{
			IFC((*pfnCreate9Ex)(D3D_SDK_VERSION, &m_pD3DEx));
			IFC(m_pD3DEx->QueryInterface(__uuidof(IDirect3D9), reinterpret_cast<void **>(&m_pD3D)));
		}


		UINT numVideoAdapters = m_pD3DEx->GetAdapterCount();
				
		CreateDevice(&m_pDevice, &m_pDeviceEx, NULL);
	}
	

Cleanup:
	if (hD3D)
	{
		FreeLibrary(hD3D);
	}

	return hr;
}



//+-----------------------------------------------------------------------------
//
//  Member:
//      CSurfaceManager::EnsureHWND
//
//  Synopsis:
//      Makes sure an HWND exists if we need it, for a specific surface
//
//------------------------------------------------------------------------------
HRESULT
CSurfaceManager::EnsureHWND(int SurfaceIndex)
{
	HRESULT hr = S_OK;

	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// surface with this index not found
		return E_ABORT;
	}

	SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

	if (!psd->hwnd)
	{
		WNDCLASS wndclass;

		wndclass.style = CS_HREDRAW | CS_VREDRAW;
		wndclass.lpfnWndProc = DefWindowProc;
		wndclass.cbClsExtra = 0;
		wndclass.cbWndExtra = 0;
		wndclass.hInstance = NULL;
		wndclass.hIcon = LoadIcon(NULL, IDI_APPLICATION);
		wndclass.hCursor = LoadCursor(NULL, IDC_ARROW);
		wndclass.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
		wndclass.lpszMenuName = NULL;
		wndclass.lpszClassName = szAppName;

		if (!RegisterClass(&wndclass))
		{
			IFC(E_FAIL);
		}

		std::wstring windowName = L"Surface_";
		windowName.append(std::to_wstring(SurfaceIndex));

		psd->hwnd = CreateWindow(szAppName,
			windowName.c_str(),
			WS_OVERLAPPEDWINDOW,
			0,                   // Initial X
			0,                   // Initial Y
			0,                   // Width
			0,                   // Height
			NULL,
			NULL,
			NULL,
			NULL);
	}

Cleanup:
	return hr;
}



//+-----------------------------------------------------------------------------
//
//  Member:
//      CSurfaceManager::CreateDevice
//
//  Synopsis:
//      Creates the device
//
//------------------------------------------------------------------------------
HRESULT
CSurfaceManager::CreateDevice(IDirect3DDevice9 **pd3dDevice, IDirect3DDevice9Ex **pd3dDeviceEx, HWND hwnd)
{	
	HRESULT hr = S_OK;

	D3DPRESENT_PARAMETERS d3dpp;
	ZeroMemory(&d3dpp, sizeof(d3dpp));
	d3dpp.Windowed = TRUE;
	d3dpp.BackBufferFormat = D3DFMT_A8R8G8B8;
	d3dpp.BackBufferHeight = 1;
	d3dpp.BackBufferWidth = 1;
	d3dpp.EnableAutoDepthStencil = TRUE;
	d3dpp.AutoDepthStencilFormat = D3DFMT_D16;
	d3dpp.SwapEffect = D3DSWAPEFFECT_DISCARD;

	D3DCAPS9 caps;
	DWORD dwVertexProcessing;
	 
	IFC(m_pD3D->GetDeviceCaps(D3DADAPTER_DEFAULT, D3DDEVTYPE_HAL, &caps));
	if ((caps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT) == D3DDEVCAPS_HWTRANSFORMANDLIGHT)
	{
		dwVertexProcessing = D3DCREATE_HARDWARE_VERTEXPROCESSING;
	}
	else
	{
		dwVertexProcessing = D3DCREATE_SOFTWARE_VERTEXPROCESSING;
	}

	if (m_pD3DEx)
	{
		IDirect3DDevice9Ex *pd3dDevice = NULL;
		IFC(m_pD3DEx->CreateDeviceEx(
			D3DADAPTER_DEFAULT,
			D3DDEVTYPE_HAL,
			hwnd,
			//dwVertexProcessing | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
			D3DCREATE_HARDWARE_VERTEXPROCESSING | D3DCREATE_MULTITHREADED,
			&d3dpp,
			NULL,
			pd3dDeviceEx
			));

		IFC((*pd3dDeviceEx)->QueryInterface(__uuidof(IDirect3DDevice9), reinterpret_cast<void**>(&pd3dDevice)));
	}
	else
	{
		assert(m_pD3D);

		IFC(m_pD3D->CreateDevice(
			D3DADAPTER_DEFAULT,
			D3DDEVTYPE_HAL,
			hwnd,
			dwVertexProcessing | D3DCREATE_MULTITHREADED | D3DCREATE_FPU_PRESERVE,
			&d3dpp,
			pd3dDevice
			));
	}

Cleanup:
	return hr;
}



//+-----------------------------------------------------------------------------
//
//  Member:
//      CRenderer::CheckDeviceState
//
//  Synopsis:
//      Returns the status of the device. 9Ex devices are a special case because 
//      TestCooperativeLevel() has been deprecated in 9Ex.
//
//------------------------------------------------------------------------------
HRESULT
CSurfaceManager::CheckDeviceState()
{
	//if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
	//	// surface with this index not found
	//	return E_ABORT;
	//}

	//SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

	if (m_pD3DEx)
	{
		//return psd->pDeviceEx->CheckDeviceState(NULL);
		return m_pDeviceEx->CheckDeviceState(NULL);
	}
	else if (m_pD3D)
	{
		//return psd->pDevice->TestCooperativeLevel();
		return m_pDevice->TestCooperativeLevel();
	}
	else
	{
		return D3DERR_DEVICELOST;
	}
}


HRESULT CSurfaceManager::CreateNewSurface(UINT uWidth, UINT uHeight, bool UseAlpha, int &index)
{
	HRESULT hr = S_OK;

	hr = EnsureD3DObjects();

	// create HWND
	WNDCLASS wndclass;

	wndclass.style = CS_HREDRAW | CS_VREDRAW;
	wndclass.lpfnWndProc = DefWindowProc;
	wndclass.cbClsExtra = 0;
	wndclass.cbWndExtra = 0;
	wndclass.hInstance = NULL;
	wndclass.hIcon = LoadIcon(NULL, IDI_APPLICATION);
	wndclass.hCursor = LoadCursor(NULL, IDC_ARROW);
	wndclass.hbrBackground = (HBRUSH)GetStockObject(WHITE_BRUSH);
	wndclass.lpszMenuName = NULL;
	std::wstring className(L"D3DClass_");
	className.append(std::to_wstring(m_nextAvailableIndex));
	wndclass.lpszClassName = className.c_str();
	std::wstring windowName(L"D3DWindow_");
	windowName.append(std::to_wstring(m_nextAvailableIndex));

	if (!RegisterClass(&wndclass))
	{
		IFC(E_FAIL);
	}

	HWND hwnd = CreateWindow(className.c_str(),
		windowName.c_str(),
		WS_OVERLAPPEDWINDOW,
		0,                   // Initial X
		0,                   // Initial Y
		0,              // Width
		0,             // Height
		NULL,
		NULL,
		NULL,
		NULL);
		

	// build new SURFACE_DATA for m_SurfaceMap
	SURFACE_DATA* psd = new SURFACE_DATA();	
	psd->Width = uWidth;
	psd->Height = uHeight;
	psd->UseAlpha = UseAlpha; 
	psd->SurfaceSettingsChanged = true;
	psd->hwnd = hwnd;
	//psd->pDevice = NULL;
	//psd->pDeviceEx = NULL;
	psd->pSurface = NULL;

	// create the device
	//IFC(CreateDevice(&psd->pDevice, &psd->pDeviceEx, hwnd));

	// create the surface
	IFC(m_pDeviceEx->CreateRenderTarget(
		psd->Width,
		psd->Height,
		psd->UseAlpha ? D3DFMT_A8R8G8B8 : D3DFMT_X8R8G8B8,
		D3DMULTISAMPLE_NONE,
		0,
		m_pDeviceEx ? FALSE : TRUE,		
		&psd->pSurface,
		NULL

		));

	IFC(m_pDeviceEx->SetRenderTarget(0, psd->pSurface));
	

	// add new surface to map
	m_SurfaceMap[m_nextAvailableIndex] = psd;

	index = m_nextAvailableIndex;
	m_nextAvailableIndex++;

Cleanup:
	return hr;
}


bool CSurfaceManager::DestroySurface(int SurfaceIndex)
{
	bool success = false;

	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// not found
		success = false;
	}
	else {
		// found
		SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

		if (psd->hwnd)
		{
			DestroyWindow(psd->hwnd);
			psd->hwnd = NULL;
			std::wstring className(L"D3DClass_");
			className.append(std::to_wstring(SurfaceIndex));
			UnregisterClass(className.c_str(), NULL);
			success = true;
		}		

		//SAFE_RELEASE(psd->pDevice);
		//SAFE_RELEASE(psd->pDeviceEx);
		SAFE_RELEASE(psd->pSurface);
		
		delete psd;

		// remove from map
		m_SurfaceMap.erase(SurfaceIndex);
	}

	return success;
}

HRESULT CSurfaceManager::LoadNewImage(int SurfaceIndex, UINT8* pImageData, UINT width, UINT height, UINT numBytes)
{  // load image from CPU memory
	HRESULT hr = S_OK;

	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// surface with this index not found
		return E_ABORT;
	}

	SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

	RECT srcRect;
	srcRect.left = 0;
	srcRect.top = 0;
	srcRect.bottom = height;
	srcRect.right = width;
	
	int bytesPerPixel = 4;

	D3DFORMAT format = psd->UseAlpha ? D3DFMT_A8R8G8B8 : D3DFMT_X8R8G8B8; 

	hr = D3DXLoadSurfaceFromMemory(psd->pSurface, NULL, NULL, pImageData, format, psd->Width*bytesPerPixel, NULL, &srcRect, D3DX_FILTER_NONE, 0);

	return hr;
}



HRESULT CSurfaceManager::LoadNewGPUImage(int SurfaceIndex, UINT8* pImageData, UINT width, UINT height, UINT numBytes)
{  // load image from CPU memory
	HRESULT hr = S_OK;

	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// surface with this index not found
		return E_ABORT;
	}

	SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];



		

	return hr;
}








//+-----------------------------------------------------------------------------
//
//  Member:
//      CSurfaceManager::GetBackBufferNoRef
//
//  Synopsis:
//      Returns the surface from the m_SurfaceMap for a given Surface Index
//
//      This can return NULL if we're in a bad device state.
//
//------------------------------------------------------------------------------
HRESULT
CSurfaceManager::GetBackBufferNoRef(int SurfaceIndex, IDirect3DSurface9 **ppSurface)
{
	HRESULT hr = S_OK;

	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// surface with this index not found
		return E_ABORT;
	}

	SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];
	
	// Make sure we at least return NULL
	*ppSurface = NULL;

	CleanupInvalidDevices();

	IFC(EnsureD3DObjects());

	*ppSurface = psd->pSurface;
	
	
Cleanup:
	// If we failed because of a bad device, ignore the failure for now and 
	// we'll clean up and try again next time.
	if (hr == D3DERR_DEVICELOST)
	{
		hr = S_OK;
	}

	return hr;
}



//+-----------------------------------------------------------------------------
//
//  Member:
//      CRendererManager::CleanupInvalidDevices
//
//  Synopsis:
//      Checks to see if any devices are bad and if so, deletes all resources
//
//      We could delete resources and wait for D3DERR_DEVICENOTRESET and reset
//      the devices, but if the device is lost because of an adapter order 
//      change then our existing D3D objects would have stale adapter 
//      information. We'll delete everything to be safe rather than sorry.
//
//------------------------------------------------------------------------------

void
CSurfaceManager::CleanupInvalidDevices()
{
	//if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
	//	// surface with this index not found
	//	return;
	//}

	//SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

	HRESULT hr;
	if (m_pDeviceEx)
	{
		hr = m_pDeviceEx->CheckDeviceState(NULL);
	}
	else if (m_pDevice)
	{
		hr = m_pDevice->TestCooperativeLevel();
	}
	else
	{
		hr = D3DERR_DEVICELOST;
	}

	if (FAILED(hr))  // destroy resources
	{
		SAFE_RELEASE(m_pD3D);
		SAFE_RELEASE(m_pD3DEx);

		std::map<int, SURFACE_DATA*>::iterator it;
		for (auto it = m_SurfaceMap.begin(); it != m_SurfaceMap.end(); ++it)
		{
			int surfIdx = it->first;
			SURFACE_DATA* ptrSurf = it->second;
			DestroySurface(surfIdx);
		}

	}

}






//+-----------------------------------------------------------------------------
//
//  Member:
//      CRendererManager::GetSurfaceData(int SurfaceIndex)
//
//  Synopsis:
//      Gets the data that is being used on the surface
//
//
//------------------------------------------------------------------------------

HRESULT
CSurfaceManager::GetSurfaceData(int SurfaceIndex, char **pImageData, int *pWidth, int *pHeight)
{
	HRESULT hr = D3DXERR_INVALIDDATA;

	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// surface with this index not found
		return D3DERR_NOTFOUND;
	}

	SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

	D3DLOCKED_RECT pRect;

	if (psd->pSurface->LockRect(&pRect, NULL, D3DLOCK_READONLY) == D3D_OK)
	{
		*pImageData = (char*)malloc(psd->Width*psd->Height * 4);
		*pWidth  = psd->Width;
		*pHeight = psd->Height;
		
		int offset1 = 0;
		int offset2 = 0;

		// copy byte data from surface 
		for (int row = 0; row < (int)psd->Height; row++)
		{
			offset1 = row * (psd->Width * 4);
			offset2 = row * pRect.Pitch;
			memcpy(*pImageData + offset1, (char*)pRect.pBits + offset2, psd->Width * 4);
		}

		psd->pSurface->UnlockRect();

		hr = S_OK;
	}	

	return hr;
	
}




//////////////////////////////////////////////////////////////////////////////////
// Getter Functions

void CSurfaceManager::GetD3D_Objects(IDirect3D9Ex** ppD3DEx, IDirect3DDevice9** ppDevice, IDirect3DDevice9Ex ** ppDeviceEx)
{
	*ppD3DEx = m_pD3DEx;
	*ppDevice = m_pDevice;
	*ppDeviceEx = m_pDeviceEx;
}


bool    CSurfaceManager::GetSurfaceParams(int SurfaceIndex, IDirect3DSurface9** ppSurface, int* pWidth, int* pHeight, bool* pUseAlpha)
{
	if (m_SurfaceMap.find(SurfaceIndex) == m_SurfaceMap.end()) {
		// surface with this index not found
		return false;
	}

	SURFACE_DATA* psd = m_SurfaceMap[SurfaceIndex];

	*ppSurface = psd->pSurface;
	*pWidth = psd->Width;
	*pHeight = psd->Height;
	*pUseAlpha = psd->UseAlpha;

	return true;
}