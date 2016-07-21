using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace CopyWallGroup.VK
{
    class VKApi
    {
        string vkuri = "https://oauth.vk.com/authorize?client_id="+Properties.Resources.idVK+"&redirect_uri=https://vk.com&scope=friends,groups,stats&response_type=token&revoke=1";

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
                if (index < 0)
                {
                    if (bro.DocumentText.ToString().IndexOf("<input type=\"text\" class=\"form_input\" name=\"email\"") > 0)
                    {
                        HtmlDocument doc = bro.Document;
                        Parallel.Invoke(() =>
                        {
                            int n = 0;
                            foreach (HtmlElement item in doc.GetElementById("box").All)
                            {
                                if (n == 0 && item.Name == "email")
                                {
                                    n = 1;
                                    item.InnerText = Properties.Settings.Default.VkLogin;
                                }
                                else if (n == 1 && item.Name == "pass")
                                {
                                    n = 2;
                                    item.InnerText = Properties.Settings.Default.VkPass;
                                    break;
                                }
                            }
                        }, () =>
                        {
                            HtmlElement btn = doc.GetElementById("install_allow");
                            if (btn.InnerText == "Войти" && 1 == 2)
                                btn.Click += new HtmlElementEventHandler(delegate(object sender_btn, HtmlElementEventArgs args)
                                {
                                    HtmlElement button = (HtmlElement)sender_btn;
                                    HtmlDocument doc_btn = button.Document;
                                    string login = doc.GetElementById("box").All.GetElementsByName("email")[0].InnerText;
                                    string password = doc.GetElementById("box").All.GetElementsByName("pass")[0].InnerText;
                                    if (login != null && password != null && login.Length > 0 && password.Length > 0)
                                    {
                                        bool ifi = false;
                                        if (Properties.Settings.Default.VkLogin != login)
                                        {
                                            Properties.Settings.Default.VkLogin = login;
                                            ifi = true;
                                        }
                                        if (Properties.Settings.Default.VkPass != password)
                                        {
                                            Properties.Settings.Default.VkPass = password;
                                            ifi = true;
                                        }
                                        if (ifi)
                                        {
                                            Properties.Settings.Default.Save();
                                        }
                                    }
                                });
                        });
                        return;
                    }
                    return;
                }
                index += 13;
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
                        Properties.Settings.Default.Save();
                        showNotifyIconInformation("Получен новый токен: " + token + " .", bro);
                    }
                }, () =>
                {
                    string tmp = e.Url.ToString();
                    int time;
                    tmp = new string(tmp.Substring(tmp.IndexOf("expires_in=") + 11).
                        TakeWhile(x => x != '&' && int.TryParse(x.ToString(), out time) == true).ToArray());
                    if (int.TryParse(tmp, out time) == true)
                    {
                        Properties.Settings.Default.VkTokenLife = DateTime.Now.AddSeconds(time);
                        Properties.Settings.Default.Save();
                        tmp = "Новый токен будет жить " + (Properties.Settings.Default.VkTokenLife - DateTime.Now) + ".";
                    }
                    else tmp = "Не удалось распознать время жизни токена в этом сообщении - " + tmp + " .";
                    showNotifyIconInformation(tmp, bro);
                });
                bro.FindForm().Close();
            });
            return web;
        }

        /// <summary>
        /// Загружает новости из группы садко
        /// </summary>
        /// <returns></returns>
        internal DataTable downloadWallNews()
        {
            DataTable table = getTableWall();

            string r = "https://api.vk.com/" +
                "method/wall.get.xml?owner_id=" + (-86525154).ToString() +
                "&count=" + 100 +
                "&extended=1" +
                "&access_token="+ Properties.Settings.Default.VkTokenValue;
            WebRequest request = WebRequest.Create(r);
            WebResponse resp = request.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader sr = new StreamReader(stream);
            string Out = sr.ReadToEnd();
            sr.Close();
            JToken token = JToken.Parse(Out);
            XmlDocument doc = new XmlDocument();
            return table;
        }

        /// <summary>
        /// Таблица для записей со стены ВК;
        /// </summary>
        /// <returns></returns>
        private DataTable getTableWall()
        {
            DataTable table = new DataTable();
            table.Columns.Add("ID post", typeof(int));
            table.Columns.Add("DateTime", typeof(DateTime));
            table.Columns.Add("Text", typeof(string));
            return table;
        }

        /// <summary>
        /// Отображает уведомление в области уведомлений
        /// </summary>
        /// <param name="text">текст уведомления</param>
        private void showNotifyIconInformation(string text)
        {
            try
            {
                if (text.Length > 63)
                    text = text.Substring(0, 60) + "...";
                NotifyIcon icon = new NotifyIcon();
                icon.Icon = System.Drawing.SystemIcons.Information;
                icon.BalloonTipText = text;
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
