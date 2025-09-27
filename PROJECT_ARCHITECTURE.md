# KAMITSUBAKI Framework 项目架构

## 项目结构概览

这是一个重构后的模块化Unity mod框架，分为以下独立的DLL项目：

### 1. KAMITSUBAKI.Framework.Core
**核心接口与抽象**
- `IMod.cs` - Mod接口定义
- `IModContext.cs` - Mod上下文接口
- `IAssetService.cs` - 资源服务接口
- `ITextService.cs` - 文本服务接口
- `GameEvents.cs` - 游戏事件系统

### 2. KAMITSUBAKI.Framework.Services
**服务实现层**
- `AssetService.cs` - VFS（虚拟文件系统）实现
- `TextService.cs` - 文本覆盖服务实现
- `ModsLoader.cs` - Mod加载器和Manifest定义
- `HarmonyPatches_AssetLoad.cs` - Asset加载的Harmony补丁

### 3. KAMITSUBAKI.Framework.UI
**UI与设置系统**
- `ISettingsWidget.cs` - 设置控件接口
- `SettingsRegistry.cs` - 设置控件注册表
- `SettingsInjector.cs` - 设置注入器
- `Widgets/DisplayModeWidget.cs` - 示例显示模式设置控件
- `Runtime/SettingsUIScanner.cs` - UI扫描器(DEBUG模式)

### 4. KAMITSUBAKI.Framework
**主框架插件**
- `FrameworkPlugin.cs` - 主框架BepInEx插件，协调所有服务

### 5. KAMITSUBAKIMod.Text
**文本处理模块**
- `TextBookMap.cs` - 简单文本替换映射
- `ScriptOverrideStore.cs` - 脚本覆盖存储
- `BookJsonRewriter.cs` - Book JSON重写器
- `DumpUtil.cs` - 调试转储工具

### 6. KAMITSUBAKIMod.Runtime
**运行时组件**
- `BookRegistry.cs` - Book对象注册表
- `BookScanner.cs` - Book扫描器
- `BookLiveRewriter.cs` - 实时Book重写器
- `BookOverrideRuntime.cs` - Book覆盖运行时
- `StoryEditorGUI.cs` - 故事编辑器GUI (F1开启)
- `VfsTestHarness.cs` - VFS测试工具

### 7. KAMITSUBAKIMod
**主Mod插件**
- `Plugin.cs` - 主mod BepInEx插件
- `Patches/` - Harmony补丁文件夹

## 依赖关系

```
KAMITSUBAKI.Framework.Core (接口层)
    ↑
KAMITSUBAKI.Framework.Services (服务实现)
    ↑
KAMITSUBAKI.Framework.UI (UI系统)
    ↑
KAMITSUBAKI.Framework (主框架)

KAMITSUBAKIMod.Text (文本处理)
    ↑
KAMITSUBAKIMod.Runtime (运行时组件)
    ↑
KAMITSUBAKIMod (主Mod)
```

## 主要功能

### VFS (虚拟文件系统)
- 支持多Mod资源覆盖
- 优先级控制
- 缓存管理
- 支持纹理、音频等多种资源类型

### 文本本地化
- TSV格式的文本覆盖文件
- 支持SimplifiedChinese和Text列
- 实时编辑和保存
- JSON字符串数组解析和重建

### 设置系统
- 模块化设置控件
- 自动UI注入
- 运行时配置

### 开发工具
- F1热键故事编辑器
- 资源扫描和转储
- 实时文本替换
- Debug模式UI扫描

## 构建输出

每个项目编译为独立的DLL，所有依赖DLL会被复制到BepInEx插件目录:
- `KAMITSUBAKI.Framework/` - 框架相关DLL
- `KAMITSUBAKIMod/` - Mod相关DLL

这种架构提供了更好的模块化、可维护性和扩展性。