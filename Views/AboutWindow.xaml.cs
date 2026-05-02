using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace CassieWordCheck.Views;

public partial class AboutWindow : Window
{
    private const string FeaturesText = @"
【核心功能】
• CASSIE 词库检查 — 逐词比对 CASSIE 配音词库，标记可用/不可用单词
• 实时统计 — 可用数、不可用数、忽略数、覆盖率一目了然
• 拼写建议 — 对不可用词自动 Levenshtein 相似词推荐
• 白名单管理 — 添加自定义豁免词，支持导入/导出
• 多语言界面 — 简体中文 / English / 日本語 / 한국어 / Deutsch / Русский

【过滤系统】
• 格式标记过滤 — 自动忽略 link、color、size、split 等 CASSIE 标签及标点符号
• 命名标记过滤 — 可屏蔽 MTF/UIU/GOC/CI/NTF/GRU/FBI 等阵营缩写及希腊字母、北约代号
• 中文忽略 — 可开关，跳过中文字符

【体验优化】
• 暗色深色主题 + Mica 毛玻璃效果（Windows 11）
• 全组件平滑动画（卡片弹入/按钮缩放/进度条过渡/逐字清空）
• 实时键入响应 — 输入即检查，结果框同步动画反馈
• 单文件自包含发布 — 无需安装 .NET 运行时

——
本项目由 AI 辅助制作
";

    private const string ChangelogText = @"
v2.1.0（代码优化）
• 新增 .sln 解决方案文件，修复 Rider/VS 无法识别项目的问题
• 修复 HexRegex 误伤纯数字的问题（要求十六进制至少含一个字母）
• Clipbaord.SetText 增加 try-catch，避免剪贴板被占用时崩溃
• 主题映射改用 switch 模式匹配，不再依赖本地化字符串作字典 key
• 移除冗余的 ViewModel 文件（MainViewModel / SettingsViewModel / WhitelistViewModel——全部是死代码）
• .csproj 重构：将单文件发布参数移至 Release 专属，避免 Debug 构建多余打包
• 版本号升级至 2.1.0

v2.0.0（第二次迭代）
• 重构 Checker 引擎：去除粗暴的 <> 整体删除，改为逐个 token 精准过滤
• 新增十六进制色值过滤（#990033 等自动忽略）
• 新增可选命名过滤系统（阵营/希腊字母/北约代号）
• 新增 pitch_ 音高标记 / .G 八度记号 / JAM 音效引用过滤
• 修复 ComboBox 下拉栏不可用的问题（完整重写模板）
• 修复白名单删除按钮失效的问题
• 全局动画系统：卡片入场弹性缩放、键入微动、进度条过渡、清空加速逐字删除
• Apple 风格卡片布局：统一 12px 圆角 + DropShadow 悬浮阴影
• 优化按钮悬停/按下缩放反馈
• 窗口标题改为 CASSIE CWC Tool（固定不跟随语言）
• 词库路径超链接，点击可直接在资源管理器中定位文件
• 工具栏 + 状态栏合并为一张卡片
• 设置页面：字体大小选择、自动换行开关
• 语言切换实时生效（界面所有文字跟随变化）
• 清空输入动画 ≤ 1.5 秒自适应加速
• 输入/结果卡片入场同步动画
• 建议面板弹出时卡片整体上移过渡

v1.0.0（初始版本）
• 基础 CASSIE 词库检查功能
• Simple dark theme
• Basic settings

——
本项目由 AI 辅助制作
";

    private bool _isFeaturesActive = true;

    public AboutWindow()
    {
        InitializeComponent();
        this.EnableDarkTitleBar();
        SetActiveTab(true);
        ContentArea.Opacity = 1;
        ContentArea.RenderTransform = new TranslateTransform(0, 0);
        ContentArea.Text = FeaturesText.TrimStart('\n', '\r');
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // 加载头像
        LoadAvatar();

        // ── 窗口入场：弹性缩放 + 淡入 ──
        var sb = new Storyboard();

        var scaleX = new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        scaleX.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(scaleX, this);
        Storyboard.SetTargetProperty(scaleX, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));

        var scaleY = new DoubleAnimation(0.92, 1, new Duration(TimeSpan.FromMilliseconds(400)));
        scaleY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(scaleY, this);
        Storyboard.SetTargetProperty(scaleY, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));

        var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        Storyboard.SetTarget(fade, this);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));

        sb.Children.Add(scaleX);
        sb.Children.Add(scaleY);
        sb.Children.Add(fade);
        sb.Begin(this);

        // ── 各卡片错开入场 ──
        AnimateElement(AppIconBorder, 0.9, 1, 80, 0, 0.1, 0.3);
        AnimateElement(ContentArea, 0.96, 1, 12, 0, 0.25, 0.35);

        // ── 底部制作者信息：从下往上 ──
        CreditsBorder.Opacity = 0;
        CreditsBorder.RenderTransform = new TranslateTransform(0, 12);
        var bFade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
        bFade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        bFade.BeginTime = TimeSpan.FromSeconds(0.35);
        CreditsBorder.BeginAnimation(OpacityProperty, bFade);

        var bMove = new DoubleAnimation(12, 0, new Duration(TimeSpan.FromMilliseconds(400)));
        bMove.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        bMove.BeginTime = TimeSpan.FromSeconds(0.35);
        CreditsBorder.RenderTransform.BeginAnimation(TranslateTransform.YProperty, bMove);
    }

    private void LoadAvatar()
    {
        try
        {
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
            var imgPath = Path.Combine(exeDir ?? ".", "qr.JPG");
            if (File.Exists(imgPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(imgPath);
                bitmap.EndInit();
                AvatarImage.Source = bitmap;
            }
        }
        catch { /* 图片加载失败静默处理 */ }
    }

    // ── 标签页切换 ────────────────────────────────────────────
    private void OnShowFeatures(object sender, MouseButtonEventArgs e) => SwitchTab(true);
    private void OnShowChangelog(object sender, MouseButtonEventArgs e) => SwitchTab(false);

    private void SwitchTab(bool toFeatures)
    {
        if (toFeatures == _isFeaturesActive) return;
        _isFeaturesActive = toFeatures;

        SetActiveTab(toFeatures);

        // 内容交叉淡出/淡入
        var newText = (toFeatures ? FeaturesText : ChangelogText).TrimStart('\n', '\r');

        // 淡出当前内容
        var fadeOut = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(180)));
        fadeOut.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn };
        fadeOut.Completed += (_, _) =>
        {
            ContentArea.Text = newText;

            // 淡入新内容
            var fadeIn = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(350)));
            fadeIn.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
            ContentArea.BeginAnimation(OpacityProperty, fadeIn);
        };
        ContentArea.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void SetActiveTab(bool toFeatures)
    {
        FeaturesTab.Tag = toFeatures ? "Active" : null;
        ChangelogTab.Tag = toFeatures ? null : "Active";
    }

    private void OnClose(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    // ── 动画辅助 ─────────────────────────────────────────────
    private static void AnimateElement(UIElement el, double fromScale, double toScale,
                                       double fromY, double toY, double delay, double duration)
    {
        el.Opacity = 0;
        el.RenderTransform = new TransformGroup
        {
            Children =
            {
                new ScaleTransform(fromScale, fromScale),
                new TranslateTransform(0, fromY)
            }
        };

        var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromSeconds(duration)));
        fade.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        fade.BeginTime = TimeSpan.FromSeconds(delay);
        el.BeginAnimation(OpacityProperty, fade);

        var moveY = new DoubleAnimation(fromY, toY, new Duration(TimeSpan.FromSeconds(duration)));
        moveY.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        moveY.BeginTime = TimeSpan.FromSeconds(delay);
        ((TransformGroup)el.RenderTransform).Children[1].BeginAnimation(TranslateTransform.YProperty, moveY);

        var scaleXAnim = new DoubleAnimation(fromScale, toScale, new Duration(TimeSpan.FromSeconds(duration)));
        scaleXAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        scaleXAnim.BeginTime = TimeSpan.FromSeconds(delay);
        ((TransformGroup)el.RenderTransform).Children[0].BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnim);

        var scaleYAnim = new DoubleAnimation(fromScale, toScale, new Duration(TimeSpan.FromSeconds(duration)));
        scaleYAnim.EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut };
        scaleYAnim.BeginTime = TimeSpan.FromSeconds(delay);
        ((TransformGroup)el.RenderTransform).Children[0].BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnim);
    }
}
