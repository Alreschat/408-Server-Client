using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RichTextBoxExtensions
{
    public static class RichTextBoxExtensions
    {
        public static void AppendText(this RichTextBox textBox, string text, Color color)
        {
            if (textBox.InvokeRequired)
            {
                textBox.BeginInvoke((MethodInvoker)delegate ()
               {
                   textBox.SelectionStart = textBox.TextLength;
                   textBox.SelectionLength = 0;

                   textBox.SelectionColor = Color.Black;
                   textBox.AppendText("[" + DateTime.Now.ToShortTimeString() + "]");
                   textBox.AppendText(" ");

                   textBox.SelectionColor = color;
                   textBox.AppendText(text);

                   textBox.AppendText(Environment.NewLine);

                   textBox.SelectionColor = textBox.ForeColor;
                   textBox.ScrollToCaret();
               });
            }
            else
            {
                textBox.SelectionStart = textBox.TextLength;
                textBox.SelectionLength = 0;

                textBox.SelectionColor = Color.Black;
                textBox.AppendText("[" + DateTime.Now.ToShortTimeString() + "]");
                textBox.AppendText(" ");

                textBox.SelectionColor = color;
                textBox.AppendText(text);

                textBox.AppendText(Environment.NewLine);

                textBox.SelectionColor = textBox.ForeColor;
                textBox.ScrollToCaret();
            }
        }
    }
}
