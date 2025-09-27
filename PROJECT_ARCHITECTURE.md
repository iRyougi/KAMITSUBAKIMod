# KAMITSUBAKI Framework ��Ŀ�ܹ�

## ��Ŀ�ṹ����

����һ���ع����ģ�黯Unity mod��ܣ���Ϊ���¶�����DLL��Ŀ��

### 1. KAMITSUBAKI.Framework.Core
**���Ľӿ������**
- `IMod.cs` - Mod�ӿڶ���
- `IModContext.cs` - Mod�����Ľӿ�
- `IAssetService.cs` - ��Դ����ӿ�
- `ITextService.cs` - �ı�����ӿ�
- `GameEvents.cs` - ��Ϸ�¼�ϵͳ

### 2. KAMITSUBAKI.Framework.Services
**����ʵ�ֲ�**
- `AssetService.cs` - VFS�������ļ�ϵͳ��ʵ��
- `TextService.cs` - �ı����Ƿ���ʵ��
- `ModsLoader.cs` - Mod��������Manifest����
- `HarmonyPatches_AssetLoad.cs` - Asset���ص�Harmony����

### 3. KAMITSUBAKI.Framework.UI
**UI������ϵͳ**
- `ISettingsWidget.cs` - ���ÿؼ��ӿ�
- `SettingsRegistry.cs` - ���ÿؼ�ע���
- `SettingsInjector.cs` - ����ע����
- `Widgets/DisplayModeWidget.cs` - ʾ����ʾģʽ���ÿؼ�
- `Runtime/SettingsUIScanner.cs` - UIɨ����(DEBUGģʽ)

### 4. KAMITSUBAKI.Framework
**����ܲ��**
- `FrameworkPlugin.cs` - �����BepInEx�����Э�����з���

### 5. KAMITSUBAKIMod.Text
**�ı�����ģ��**
- `TextBookMap.cs` - ���ı��滻ӳ��
- `ScriptOverrideStore.cs` - �ű����Ǵ洢
- `BookJsonRewriter.cs` - Book JSON��д��
- `DumpUtil.cs` - ����ת������

### 6. KAMITSUBAKIMod.Runtime
**����ʱ���**
- `BookRegistry.cs` - Book����ע���
- `BookScanner.cs` - Bookɨ����
- `BookLiveRewriter.cs` - ʵʱBook��д��
- `BookOverrideRuntime.cs` - Book��������ʱ
- `StoryEditorGUI.cs` - ���±༭��GUI (F1����)
- `VfsTestHarness.cs` - VFS���Թ���

### 7. KAMITSUBAKIMod
**��Mod���**
- `Plugin.cs` - ��mod BepInEx���
- `Patches/` - Harmony�����ļ���

## ������ϵ

```
KAMITSUBAKI.Framework.Core (�ӿڲ�)
    ��
KAMITSUBAKI.Framework.Services (����ʵ��)
    ��
KAMITSUBAKI.Framework.UI (UIϵͳ)
    ��
KAMITSUBAKI.Framework (�����)

KAMITSUBAKIMod.Text (�ı�����)
    ��
KAMITSUBAKIMod.Runtime (����ʱ���)
    ��
KAMITSUBAKIMod (��Mod)
```

## ��Ҫ����

### VFS (�����ļ�ϵͳ)
- ֧�ֶ�Mod��Դ����
- ���ȼ�����
- �������
- ֧��������Ƶ�ȶ�����Դ����

### �ı����ػ�
- TSV��ʽ���ı������ļ�
- ֧��SimplifiedChinese��Text��
- ʵʱ�༭�ͱ���
- JSON�ַ�������������ؽ�

### ����ϵͳ
- ģ�黯���ÿؼ�
- �Զ�UIע��
- ����ʱ����

### ��������
- F1�ȼ����±༭��
- ��Դɨ���ת��
- ʵʱ�ı��滻
- DebugģʽUIɨ��

## �������

ÿ����Ŀ����Ϊ������DLL����������DLL�ᱻ���Ƶ�BepInEx���Ŀ¼:
- `KAMITSUBAKI.Framework/` - ������DLL
- `KAMITSUBAKIMod/` - Mod���DLL

���ּܹ��ṩ�˸��õ�ģ�黯����ά���Ժ���չ�ԡ�