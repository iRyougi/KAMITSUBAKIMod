# 神椿市建设中。 游戏补丁以及MOD开发企划

<p align="center">
  <img src="https://www.iryougi.com/wp-content/uploads/2025/09/1758546227-ezgif.com-crop.webp" width="300" /> <br>
</p>

## MOD框架开发企划 - 当前首要任务

### 已实现

- 剧情文本（.book）覆盖：加载时扫描 JSON 中 strings 数组，用 scripts/*.book.override.tsv 覆盖 Text 或 SimplifiedChinese 列。
- 贴图类资源（立绘 / 背景 / UI 图标等）替换：若游戏通过 AssetBundle.LoadAsset(string, Type) 或 Resources.Load(string, Type) 请求 Texture2D / Sprite，并且在任一挂载的 assets/ 下存在同名相对路径 png，则可命中替换。
- 多 Mod 挂载与优先级：mod.json mounts 支持多个目录，优先级高的先命中。

### 部分实现
- Text 热更新：需重启 / 重新加载对象才能生效，没有文件监控与实时 re-apply。
- 立绘/背景/图标的命名依赖：必须与加载调用的虚拟路径完全匹配（无别名/映射）。
- 冲突日志：只打印第一次命中，没有专门冲突决策信息。
- 缓存控制：已有清理与单条移除，但未做自动失效或按修改时间刷新。

### 未实现
- 音频替换：AudioClip 分支返回 null，实际尚未加载任何声音格式。
- 复杂资源（Prefab、材质、Spine 等）结构内引用的深层纹理自动替换（当前仅直接显式加载的 Texture2D/Sprite）。
- 文本 TSV 容错（BOM / 空列 / 列名缺失）与简化导出命令。
- 热重载(FileSystemWatcher)、调试命令面板、冲突分析、API 事件（如 OnBookRegistered）。
