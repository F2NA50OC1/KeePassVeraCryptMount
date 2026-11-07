// Fake VeraCrypt.exe for testing the plugin on Windows/Wine: logs its command line and shows a
// password dialog (class #32770, EDIT with ES_PASSWORD + COMBOBOX, like VeraCrypt's IDD_PASSWORD_DLG).
// FAKEVC_MODE=nowindow: no window at all;  FAKEVC_MODE=msgbox: an error box (focus on a button);
// FAKEVC_MODE=tokenpin: only a security token PIN dialog (like IDD_TOKEN_PASSWORD) - the plugin
// must not type the volume password into it.
// FAKEVC_MODE=pimfocus: the password dialog with the cursor in its PIM field - the plugin must not
// type the password there (it waits for the cursor to be in the password field).
// On OK: typed.txt = password field, pim.txt = PIM field. Control IDs as in VeraCrypt (fakevc.rc).
#include <windows.h>
#include <shellapi.h>
#include <stdio.h>
#include <wchar.h>
#define IDC_PASSWORD 1035
#define IDC_PIM 5130
#define IDC_TOKEN_PASSWORD 5102
static int g_dialog = 1;
static int g_pimfocus = 0;

static wchar_t g_dir[MAX_PATH];

static void write_utf8(const wchar_t *name, const wchar_t *text, int append) {
    wchar_t path[MAX_PATH];
    swprintf(path, MAX_PATH, L"%ls\\%ls", g_dir, name);
    FILE *f = _wfopen(path, append ? L"ab" : L"wb");
    if (!f) return;
    char buf[8192];
    int n = WideCharToMultiByte(CP_UTF8, 0, text, -1, buf, sizeof(buf), NULL, NULL);
    if (n > 1) fwrite(buf, 1, n - 1, f);
    fclose(f);
}

static INT_PTR CALLBACK DlgProc(HWND h, UINT msg, WPARAM wp, LPARAM lp) {
    switch (msg) {
    case WM_INITDIALOG:
        if (g_dialog == 1) SetWindowTextW(h, L"Enter password for test volume.hc");
        if (g_pimfocus) { ShowWindow(GetDlgItem(h, IDC_PIM), SW_SHOW); SetFocus(GetDlgItem(h, IDC_PIM)); }
        else SetFocus(GetDlgItem(h, g_dialog == 1 ? IDC_PASSWORD : IDC_TOKEN_PASSWORD));
        SetForegroundWindow(h);
        return FALSE; /* focus set manually */
    case WM_COMMAND:
        if (LOWORD(wp) == IDOK) {
            wchar_t pw[512] = {0};
            GetDlgItemTextW(h, g_dialog == 1 ? IDC_PASSWORD : IDC_TOKEN_PASSWORD, pw, 512);
            write_utf8(L"typed.txt", pw, 0);
            if (g_dialog == 1) { wchar_t pim[512] = {0}; GetDlgItemTextW(h, IDC_PIM, pim, 512); write_utf8(L"pim.txt", pim, 0); }
            EndDialog(h, IDOK);
            return TRUE;
        }
        if (LOWORD(wp) == IDCANCEL) { EndDialog(h, IDCANCEL); return TRUE; }
        break;
    }
    return FALSE;
}

int WINAPI wWinMain(HINSTANCE hi, HINSTANCE hp, PWSTR cmd, int show) {
    GetModuleFileNameW(NULL, g_dir, MAX_PATH);
    wchar_t *slash = wcsrchr(g_dir, L'\\'); if (slash) *slash = 0;

    int argc = 0;
    LPWSTR *argv = CommandLineToArgvW(GetCommandLineW(), &argc);
    write_utf8(L"argv.txt", L"", 0);
    for (int i = 1; i < argc; i++) { write_utf8(L"argv.txt", argv[i], 1); write_utf8(L"argv.txt", L"\n", 1); }
    wchar_t pid[32]; swprintf(pid, 32, L"%lu", GetCurrentProcessId()); write_utf8(L"pid.txt", pid, 0);

    wchar_t mode[32] = {0};
    GetEnvironmentVariableW(L"FAKEVC_MODE", mode, 32);
    if (wcscmp(mode, L"nowindow") == 0) { Sleep(25000); return 0; }
    if (wcscmp(mode, L"msgbox") == 0) { MessageBoxW(NULL, L"The volume is already mounted.", L"VeraCrypt", MB_OK | MB_ICONERROR); return 0; }
    if (wcscmp(mode, L"tokenpin") == 0) g_dialog = 2;
    if (wcscmp(mode, L"pimfocus") == 0) g_pimfocus = 1;
    DialogBoxParamW(hi, MAKEINTRESOURCEW(g_dialog), NULL, DlgProc, 0);
    return 0;
}
