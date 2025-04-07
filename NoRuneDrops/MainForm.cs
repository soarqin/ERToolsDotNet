using System.Windows.Forms;

namespace NoRuneDrops;

public partial class MainForm : Form
{
    private Button? btnSelectDirectory;
    private TextBox? txtDirectory;
    private Button? btnGenerate;
    private CheckBox? chkNoGetSoul;
    private CheckBox? chkNoDropGoldRune;
    private CheckBox? chkNoPickGoldRune;
    private CheckBox? chkCelebrantWeaponsOnStartup;

    public MainForm()
    {
        InitializeComponent();
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Set form properties
        this.Text = "No Rune Drops";
        this.Size = new Size(600, 300);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.FixedSingle;
        this.MaximizeBox = false;

        // Create and configure directory selection button
        btnSelectDirectory = new Button
        {
            Text = "选择游戏目录",
            Location = new Point(20, 20),
            Size = new Size(100, 30)
        };
        btnSelectDirectory.Click += BtnSelectDirectory_Click;

        // Create and configure directory textbox
        txtDirectory = new TextBox
        {
            Location = new Point(140, 20),
            Size = new Size(420, 30),
            ReadOnly = true
        };

        // Create and configure checkboxes
        chkNoGetSoul = new CheckBox
        {
            Text = "击杀敌人不获得卢恩并且Boss追忆不可使用",
            Location = new Point(20, 70),
            Size = new Size(360, 30),
            Checked = true
        };

        chkNoDropGoldRune = new CheckBox
        {
            Text = "敌人不掉落黄金卢恩",
            Location = new Point(20, 100),
            Size = new Size(360, 30),
            Checked = true
        };

        chkNoPickGoldRune = new CheckBox
        {
            Text = "替换所有可以拾取的黄金卢恩为各种随机材料",
            Location = new Point(20, 130),
            Size = new Size(360, 30),
            Checked = true
        };

        chkCelebrantWeaponsOnStartup = new CheckBox
        {
            Text = "初始角色获得全套庆典武器",
            Location = new Point(20, 160),
            Size = new Size(360, 30),
            Checked = true
        };

        // Create and configure generate button
        btnGenerate = new Button
        {
            Text = "生成",
            Location = new Point(460, 200),
            Size = new Size(100, 30),
            Enabled = false
        };
        btnGenerate.Click += BtnGenerate_Click;

        // Add controls to form
        this.Controls.Add(btnSelectDirectory);
        this.Controls.Add(txtDirectory);
        this.Controls.Add(chkNoGetSoul);
        this.Controls.Add(chkNoDropGoldRune);
        this.Controls.Add(chkNoPickGoldRune);
        this.Controls.Add(chkCelebrantWeaponsOnStartup);
        this.Controls.Add(btnGenerate);
    }

    private void BtnSelectDirectory_Click(object? sender, EventArgs e)
    {
        using FolderBrowserDialog folderDialog = new();
        if (folderDialog.ShowDialog() == DialogResult.OK)
        {
            txtDirectory!.Text = folderDialog.SelectedPath;
            btnGenerate!.Enabled = !string.IsNullOrEmpty(txtDirectory.Text);
        }
    }

    private void BtnGenerate_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtDirectory!.Text))
        {
            MessageBox.Show("请先选择游戏目录！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Generator.Generate(txtDirectory.Text, chkNoGetSoul!.Checked, chkNoDropGoldRune!.Checked, chkNoPickGoldRune!.Checked, chkCelebrantWeaponsOnStartup!.Checked);
            MessageBox.Show("生成成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"生成失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
