using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyWallGroup.VK
{
    class VKApi
    {
        string vkuri = "https://oauth.vk.com/authorize?client_id="+Properties.Resources.idVK+"&redirect_uri=http://vk.com&scope=friends,groups,stats&response_type=token&revoke=1";

        private System.Windows.Forms.WebBrowser getWebBroserControl()
        {
            WebBrowser web = new WebBrowser();
            web.Url = new Uri(vkuri);
            web.Dock = System.Windows.Forms.DockStyle.Fill;
            web.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(
                (object sender, WebBrowserDocumentCompletedEventArgs e) =>
            {
                WebBrowser bro = (WebBrowser)sender;
                int index = e.Url.ToString().IndexOf("access_token=");
                if (index < 0) return;
                index += "access_token=".Length;
                string oldtoken = Properties.Settings.Default.VkTokenValue;
                DateTime oldTime = Properties.Settings.Default.VkTokenLife;
                Parallel.Invoke(() =>
                {
                    string token = e.Url.ToString();
                    token = new string(token.Substring(index).TakeWhile(x => x != '&').ToArray());
                    DialogResult r = MessageBox.Show("Сохранить новый токент для VK = " + token + "?", "Вопрос",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                    if (r == DialogResult.Yes)
                    {
                        Properties.Settings.Default.VkTokenValue = token;
                        showNotifyIconInformation("Получен новый токен: " + token + " .", bro);
                    }
                }, () =>
                {
                    string tmp = e.Url.ToString();
                    int time;
                    tmp = new string(tmp.Substring(tmp.IndexOf("expires_in=")).
                        TakeWhile(x => x != '&' && int.TryParse(x.ToString(), out time) == true).ToArray());
                    if (int.TryParse(tmp, out time) == true)
                    {
                        Properties.Settings.Default.VkTokenLife = DateTime.Now.AddSeconds(time);
                        tmp = "Новый токен будет жить " + (Properties.Settings.Default.VkTokenLife - DateTime.Now) + ".";
                    }
                    else tmp = "Не удалось распознать токен в этом сообщении - " + tmp + " .";
                    showNotifyIconInformation(tmp, bro);
                });
            });
            return web;
        }

        /// <summary>
        /// Отображает уведомление в области уведомлений
        /// </summary>
        /// <param name="text">текст уведомления</param>
        private void showNotifyIconInformation(string text)
        {
            try
            {
                NotifyIcon icon = new NotifyIcon();
                icon.Icon = System.Drawing.SystemIcons.Information;
                icon.Text = text;
                icon.ShowBalloonTip(3000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "", MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Отображает уведомление в области уведомлений
        /// </summary>
        /// <param name="text">текст уведомления</param>
        /// <param name="control">Может быть равен нулю</param>
        private void showNotifyIconInformation(string text, Control control)
        {
            control.Invoke(new MethodInvoker(()=> { showNotifyIconInformation(text); }));
        }

        /*
* http://REDIRECT_URI#access_token=533bacf01e11f55b536a565b57531ad114461ae8736d6506a3&expires_in=86400&
* user_id=8492&state=123456
* 
* http://REDIRECT_URI#error=access_denied&
* error_description=The+user+or+authorization+server+denied+the+request.
*/
        private string getWebWindow()
        {
            System.Windows.Forms.Form web = new System.Windows.Forms.Form();
            web.Width = 800; web.Height = 600;
            web.Text = "Авторизация";
            web.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            var browser = getWebBroserControl();
            web.Controls.Add(browser);
            web.ShowDialog();
            return Properties.Settings.Default.VkTokenValue;
        }

        private bool provToken()
        {
            if (Properties.Settings.Default.VkTokenValue == null)
                return false;
            if (Properties.Settings.Default.VkTokenValue.Length == 0)
                return false;
            if (Properties.Settings.Default.VkTokenLife == null)
                return false;
            if (Properties.Settings.Default.VkTokenLife > DateTime.Now)
                return false;
            if ((Properties.Settings.Default.VkTokenLife - DateTime.Now).Minutes < 1)
                return false;
            return false;
        }

        internal string oauth()
        {
            switch(provToken())
            {
                case true:
                    return Properties.Settings.Default.VkTokenValue;
                default:
                    return getWebWindow();
            }
        }
    }
}
