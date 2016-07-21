using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CopyWallGroup
{
    public partial class MainWindow : Form
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            System.Threading.Thread thread = new System.Threading.Thread(() =>
            {
                vkstart();
            });
            thread.Name = "Авторизация и загрузка новостей VK";
            thread.Start();
        }

        private void vkstart()
        {
            VK.VKApi vk = new VK.VKApi();
            Invoke(new MethodInvoker(()=> { vk.oauth(); }));
            if (Properties.Settings.Default.VkTokenLife > DateTime.Now)
            {
                DataTable table = vk.downloadWallNews();
            }
        }
    }
}
