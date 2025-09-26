# 《神椿市建设中。》 游戏补丁以及MOD开发企划

<p align="center">
  <img src="https://www.iryougi.com/wp-content/uploads/2025/09/1758546227-ezgif.com-crop.webp" width="300" /> <br>
</p>
### 当前进度

- [x] 剧本编辑器 - 基本可用
- [x] 剧本提取器 - 基本可用
- [ ] KAMITSUBAKI.Framwork MOD框架 - 初期开发

**本项目暂时只有本人一人开发，有兴趣的朋友可以联系我加入项目开发，本项目急缺人手**

©️2025 KAMITSUBAKI METAVERSE R&D DIV by [iRyougi](www.iryougi.com)

## 安装

本框架与模组基于[BepInEx (x86/x64/Unity)](https://github.com/BepInEx/BepInEx)注入器开发

### 1）安装注入器
1. 备份游戏目录。
2. 把 **BepInEx** 解压到游戏根目录（与游戏 exe 同层）。
3. 运行游戏一次，自动生成结构：
   ```txt
   BepInEx/
     core/
     config/
     plugins/
     patchers/
   doorstop_config.ini
   winhttp.dll
   ```

### 2）插件安装

将编译完成的插件放入BepInEx/plugins文件夹（后续有计划制作MOD管理器（处于新建文件夹阶段））

启动游戏，自动载入MOD

## 剧本提取器

将编译插件按照安装流程放入插件文件夹

启动游戏，自动生成剧本文件（json格式）以及三语对照表（tsv格式）

> [!NOTE]
>
> 三语对照表指日文原文（text），简体中文（SimplifyChinese），英文（English）

将tsv导入Excel即可获得剧本（怎么导入请自行百度）

## 剧本编辑器

将编译插件按照安装流程放入插件文件夹

启动游戏，在游戏界面按F1，调用剧本编辑器（游戏内覆盖）

说明待更新

## KAMITSUBAKI.Framwork MOD框架

这是一个雄心勃勃的企划

在Mod框架的帮助下，您可以轻松实现对剧情文本、立绘、背景、音频、甚至游戏图标的替换

并且Mod框架使用简单，经过短暂学习即可上手

项目暂时处于初期阶段，欢迎热爱神椿的你的加入
