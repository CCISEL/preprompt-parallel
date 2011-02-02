using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FindInFiles.SearchBehaviors;

namespace FindInFiles
{
    public partial class Form1 : Form
    {
        private readonly ISearchBehavior _behavior;
        private CancellationTokenSource _cts;

        public Form1(ISearchBehavior behavior)
        {
            _behavior = behavior;
            InitializeComponent();
        }

        private void cancel_button_click(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
            }
        }

        private void search_button_click(object sender, EventArgs e)
        {
            string directory = _tbDir.Text;
            string pattern = _tbPattern.Text;

            _lbResults.Items.Clear();

            if (string.IsNullOrEmpty(directory) || string.IsNullOrEmpty(pattern))
            {
                MessageBox.Show("Missing directory or pattern.");
                return;
            }

            bool ignoreCase = pattern.Contains("/i");

            var nonSwitches = pattern.Split(' ').Where(pat => pat.Any() && pat.First() != '/');
            var regexString = nonSwitches.FirstOrDefault();

            if (regexString == null)
            {
                MessageBox.Show("No valid pattern was provided.");
                return;
            }

            var wildcards = nonSwitches.Skip(1).ToArray();
            if (wildcards.Length == 0)
            {
                wildcards = new[] { "*.*" };
            }

            _btnSearch.Enabled = false;
            _btnCancel.Enabled = true;

            var regex = new Regex(regexString, RegexOptions.Compiled | (ignoreCase
                                               ? RegexOptions.IgnoreCase
                                               : RegexOptions.None));

            _cts = new CancellationTokenSource();
            _behavior.Start(directory, wildcards, regex, _cts.Token, matches =>
                {
                    if (matches != null)
                    {
                        _lbResults.Items.AddRange(matches);
                    }
                }, ellapsed =>
                {
                    _btnSearch.Enabled = true;
                    _btnCancel.Enabled = false;
                    MessageBox.Show(string.Format("Done: {0}", ellapsed));
                });
        }
    }
}