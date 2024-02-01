using Microsoft.Extensions.Logging;

namespace WinForms;

public partial class Form1 : Form
{
    private readonly ILogger<Form1> _logger;

    public Form1(ILogger<Form1> logger)
    {
        _logger = logger;
        InitializeComponent();
    }
}
