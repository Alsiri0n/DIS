using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace DIS
{
    public partial class DIS : Form
    {
        private const string waitAddressText = "Ожидаем ввода адреса.";
        private const string loadingFileText = "Загружаем файлы...";
        private const string allImageDownloaded = "Все изображения загружены.";
        private const string sourceText = "/src/";
        private const string dirDelimeter = "\\";
        private const string htmlTagNameText = "span";
        private const string htmlClass = "classname";
        private const string htmlAttribute = "filesize";
        private const char delimeterHtmlAddress = '/';
        private const string entrance2chFirst = "http://2ch.hk/";
        private const string entrance2chSecond = ".html";
        private const int WM_DRAWCLIPBOARD = 0x0308;
        private const int WM_CHANGECBCHAIN = 0x030D;
        
        private string dirpath;
        private string filepath;
        private string[] sa;

        private IntPtr nextClipboardViewer;

        public DIS()
        {
            InitializeComponent();
            webBrowser1.ScriptErrorsSuppressed = true;
            toolStripStatusLabel1.Text = waitAddressText;
            WindowState = FormWindowState.Minimized;
        }

        private void DIS_Load(object sender, EventArgs e)
        {
            chBox_Spy_CheckedChanged(sender, e);
        }
        
        private void btn_Download_Click(object sender, EventArgs e)
        {
            downloadImage();
        }

        private void downloadImage()
        {
            if (txBox_url.Text != "")
            {
                webBrowser1.Url = new Uri(txBox_url.Text);
                webBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(WebBrowser_DocumnetCompleted);
                sa = txBox_url.Text.Substring(7).Split(delimeterHtmlAddress);
            }
        }

        private void WebBrowser_DocumnetCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            toolStripStatusLabel1.Text = loadingFileText;
            HtmlElementCollection theElementCollection = default(HtmlElementCollection);
            theElementCollection = webBrowser1.Document.GetElementsByTagName(htmlTagNameText);
            dirpath = sa[1] + dirDelimeter + sa[3].Split('.')[0];
            System.IO.Directory.CreateDirectory(dirpath);
            toolStripProgressBar1.Maximum = theElementCollection.Count+1;

            foreach (HtmlElement curElement in theElementCollection)
            {
                toolStripProgressBar1.Value++;
                if (curElement.GetAttribute(htmlClass).ToString() == htmlAttribute)
                {
                    string s = "http://" + sa[0] + delimeterHtmlAddress + sa[1] + sourceText + curElement.InnerText.Split(' ')[0];
                    System.Net.WebClient wc = new System.Net.WebClient();

                    filepath = Environment.CurrentDirectory + dirDelimeter + dirpath + dirDelimeter + curElement.InnerText.Split(' ')[0];
                    if (System.IO.File.Exists(filepath))
                    {
                        continue;
                    }
                    wc.DownloadFileAsync(new Uri(s), @filepath);
                }

            }
            toolStripStatusLabel1.Text = allImageDownloaded;
            toolStripProgressBar1.Value = 0;
            notifyIcon1.ShowBalloonTip(300, "Уведомление", "Скачено.", ToolTipIcon.None);
        }

        private void statusStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", @Environment.CurrentDirectory + dirDelimeter + dirpath);
        }

        private void chBox_Spy_CheckedChanged(object sender, EventArgs e)
        {
            if (chBox_Spy.Checked)
            {
                nextClipboardViewer = SetClipboardViewer(this.Handle);
            }
            else
            {
                ChangeClipboardChain(this.Handle, nextClipboardViewer);
            }
        }


        public string SwapClipboardHtmlText(string replacementHtmlText)
        {
            String returnHtmlText = null;
            if (Clipboard.ContainsText(TextDataFormat.Html))
            {
                returnHtmlText = Clipboard.GetText(TextDataFormat.Html);
            }
            return returnHtmlText;
        }


        //Register a window handle as a clipboard viewer
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetClipboardViewer(IntPtr hWnd);
        //Remove a window handle from the clipboard viewers chain
        [DllImport("User32.dll", CharSet = CharSet.Auto)]
        public static extern bool ChangeClipboardChain(
            IntPtr hWndRemove,  // handle to window to remove
            IntPtr hWndNewNext  // handle to next window
            );
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_DRAWCLIPBOARD:
                    //the contents of the clipboard have changed
                    {
                        //Process clipboard change
                        ClipboardChanged();
                        //Send the message to the next window
                        SendMessage(nextClipboardViewer, WM_DRAWCLIPBOARD, IntPtr.Zero, IntPtr.Zero);
                        break;
                    }
                case WM_CHANGECBCHAIN:
                    //the clipboard chain has changed and we have to pass the news along
                    {
                        if (m.WParam == nextClipboardViewer)
                        {
                            //the window we've been passing WM_DRAWCLIPBOARD to has been removed
                            //from the chain, so we have to update our message target
                            nextClipboardViewer = m.LParam;
                        }
                        else
                        {
                            //just pass along the message
                            SendMessage(nextClipboardViewer, WM_CHANGECBCHAIN, m.WParam, m.LParam);
                        }
                        m.Result = IntPtr.Zero;
                        break;
                    }
                default:
                    {
                        base.WndProc(ref m);
                        break;
                    }
            }
        }

        private void ClipboardChanged()
        {
            if (Clipboard.ContainsText() && Clipboard.GetText().Length>14 &&
                Clipboard.GetText().Substring(0, entrance2chFirst.Length) == entrance2chFirst &&
                Clipboard.GetText().Substring(Clipboard.GetText().Length - entrance2chSecond.Length, entrance2chSecond.Length) == entrance2chSecond)
            {
                txBox_url.Text = Clipboard.GetText();

                if (FormWindowState.Minimized == WindowState)
                    notifyIcon1.ShowBalloonTip(300, "Уведомление", "Скачать?", ToolTipIcon.None);
            }
        }

        private void toTrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void DIS_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                notifyIcon1.Visible = true;
                this.WindowState = FormWindowState.Minimized;
                this.ShowInTaskbar = false;
                this.Hide();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            // Разворачиваем окно
            WindowState = FormWindowState.Normal;
            // Прячем за собой иконку в трее
            notifyIcon1.Visible = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("https://github.com/IgnatievN/DIS/", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
                downloadImage();
        }
    }
}
