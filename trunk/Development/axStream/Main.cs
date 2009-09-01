using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.Threading;
using System.IO;
using axStream.Properties;
using ZeroconfService;
using System.Collections;
using System.Net;

namespace axStream
{
    public partial class Main : Form
    {
        private NetService selectedService = null;

        public Main()
        {
            InitializeComponent();

            
        }

        private Player pl;

        private void Main_Load(object sender, EventArgs e)
        {
            //this.Hide();
            nsBrowser.InvokeableObject = this;
            nsBrowser.DidFindService += new NetServiceBrowser.ServiceFound(nsBrowser_DidFindService);
            nsBrowser.DidRemoveService += new NetServiceBrowser.ServiceRemoved(nsBrowser_DidRemoveService);

            if (!mBrowsing)
            {

                nsBrowser.SearchForService("_raop._tcp", "");

                mBrowsing = true;
            }
            else
            {
                nsBrowser.Stop();

                if (resolving != null)
                {
                    resolving.Stop();
                    resolving = null;
                }
                ClearResolveInfo();
                servicesList.BeginUpdate();
                servicesList.Items.Clear();
                servicesList.EndUpdate();

                mBrowsing = false;
            }

            VolumeBar.Value = int.Parse(DSInfo.Tables[0].Rows[0].ItemArray[1].ToString());

            notifyIcon.Text = "";

            contextMenu.Items[0].Visible = true;
            contextMenu.Items[1].Visible = false;
            contextMenu.Items[3].Enabled = true;
            VolumeGB.Enabled = false;
            ConnectionGB.Enabled = true;
            StartButton.Enabled = true;
            StopButton.Enabled = false;

          
        }

        private void OnConnect(object sender, EventArgs e)
        {
            StatusLabel.Text = "Connected";
            notifyIcon.Icon = Resources.streaming;
            notifyIcon.Text = "Streaming to: " + selectedService.HostName;
            notifyIcon.ShowBalloonTip(2000,
                                      "Connected",
                                      "Connected successfully to: " + selectedService.HostName, 
                                      ToolTipIcon.Info);

            DoConnectControls();
        }

        private void DoConnectControls()
        {
            contextMenu.Items[0].Visible = false;
            contextMenu.Items[1].Visible = true;
            VolumeGB.Enabled = true;
            ConnectionGB.Enabled = false;
            StartButton.Enabled = false;
            StopButton.Enabled = true;
        }

        private void OnDisconnect(object sender, EventArgs e)
        {
            StatusLabel.Text = "Not connected";
            notifyIcon.Icon = Resources.inactive;
            notifyIcon.Text = "";
            notifyIcon.ShowBalloonTip(2000,
                          "Disconnected",
                          "Disconnected from: " + selectedService.HostName,
                          ToolTipIcon.Info);

            DoDisconnectControl();
        }

        private void OnError(object sender, axStream.Player.ErrorEventArgs e)
        {
            StatusLabel.Text = "Error";
            notifyIcon.Icon = Resources.error;

            if (e.Error == Player.ErrorEventArgs.ERRORNUMBER.ERRORCONNECTING)
            {
                notifyIcon.Text = "Error connecting to: ";// +IPTextBox.Text + "\n" + e.Exception.ToString();
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "Could not connect to: " + selectedService.HostName,
                          ToolTipIcon.Error);
            }
            else if (e.Error == Player.ErrorEventArgs.ERRORNUMBER.ERRORSENDING)
            {
                notifyIcon.Text = "Error sending to: ";// +IPTextBox.Text + "\n" + e.Exception.ToString();
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "Transmission was interrupted when sending to: " + selectedService.HostName,
                          ToolTipIcon.Error);
            }
            else if (e.Error == Player.ErrorEventArgs.ERRORNUMBER.ERRORRECORDING)
            {
                notifyIcon.Text = "Record error: ";// +e.Exception.ToString();
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "Error recording from sound card",
                          ToolTipIcon.Error);
            }
            else
            {
                notifyIcon.Text = "Error...";
                notifyIcon.ShowBalloonTip(2000,
                          "Error",
                          "An unknown error occured",
                          ToolTipIcon.Error);
            }

            DoDisconnectControl();
        }

        private void DoDisconnectControl()
        {
            contextMenu.Items[0].Visible = true;
            contextMenu.Items[1].Visible = false;
            VolumeGB.Enabled = false;
            ConnectionGB.Enabled = true;
            StartButton.Enabled = true;
            StopButton.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartPlaying();
        }

        private void StartPlaying()
        {
            DoConnectControls();

            StatusLabel.Text = "Connecting...";
            notifyIcon.Icon = Resources.active;

            pl = new Player(selectedService.HostName, Convert.ToDouble(VolumeBar.Value));

            pl.OnConnect += new Player.OnConnectEventHandler(this.OnConnect);
            pl.OnDisconnect += new Player.OnDisconnectEventHandler(this.OnDisconnect);
            pl.OnError += new Player.OnErrorEventHandler(this.OnError);

            //Thread starter = new Thread(new ThreadStart(pl.Start));
            //starter.Start();
            pl.Start();

            /*if (pl.Connected)
            {
                StatusLabel.Text = "Connected";
                notifyIcon.Icon = GetIcon("active.ico");
                notifyIcon.Icon = GetIcon("streaming.ico");

                contextMenu.Items[0].Visible = false;
                contextMenu.Items[1].Visible = true;
                VolumeGB.Enabled = true;
                ConnectionGB.Enabled = false;
                StartButton.Enabled = false;
                StopButton.Enabled = true;
            }
            else
            {
                notifyIcon.Icon = GetIcon("error.ico");
            }*/
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopPlaying();
        }

        private void StopPlaying()
        {
            if (pl != null) pl.Stop();

            pl = null;
            StatusLabel.Text = "Not connected";
            notifyIcon.Icon = Resources.inactive;
            notifyIcon.Text = "";

            DoDisconnectControl();
        }

        private void VolumeBar_Scroll(object sender, EventArgs e)
        {
            pl.SetVolume(VolumeBar.Value);
        }

        protected override void OnResize(EventArgs e)
        {
            //hide the form 
            if (this.WindowState == FormWindowState.Minimized)
            {
                contextMenu.Items[3].Enabled = true;
                this.Hide();
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                base.OnResize(e);
            }
        }

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            contextMenu.Items[3].Enabled = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        protected override void OnClosed(EventArgs e)
        {
            ExitApplication();
            base.OnClosed(e);
        }

        private void ExitApplication()
        {
            StopPlaying();
            notifyIcon.Visible = false;
            Application.Exit();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            StartPlaying();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            StopPlaying();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            ExitApplication();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }

        private void aboutOAEPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new AboutBox().ShowDialog(this);
        }


         NetServiceBrowser nsBrowser = new NetServiceBrowser();
        bool mBrowsing = false;

        void nsBrowser_DidRemoveService(NetServiceBrowser browser, NetService service, bool moreComing)
        {
            servicesList.BeginUpdate();

            foreach (ListViewItem item in servicesList.Items)
            {
                if (item.Tag == service)
                    servicesList.Items.Remove(item);
            }

            servicesList.EndUpdate();
        }

        ArrayList waitingAdd = new ArrayList();
        void nsBrowser_DidFindService(NetServiceBrowser browser, NetService service, bool moreComing)
        {
            ListViewItem item = new ListViewItem(service.Name);
            item.Tag = service;

            if (moreComing)
            {
                waitingAdd.Add(item);
            }
            else
            {
                servicesList.BeginUpdate();
                while (waitingAdd.Count > 0)
                {
                    servicesList.Items.Add((ListViewItem)waitingAdd[0]);
                    waitingAdd.RemoveAt(0);
                }
                servicesList.Items.Add(item);
                servicesList.EndUpdate();
            }
        }

        private void startStopButton_Click(object sender, EventArgs e)
        {
           
        }

        private void ClearResolveInfo()
        {
            servicesList.Visible = false;

            servicesList.BeginUpdate();
            servicesList.Items.Clear();
            servicesList.EndUpdate();
        }

        NetService resolving = null;
        private void Resolve(NetService resolve)
        {
            if (resolving != null)
            {
                resolving.Stop();
            }

            resolve.DidResolveService += new NetService.ServiceResolved(resolve_DidResolveService);
            resolve.ResolveWithTimeout(10); /* FIXME timeout doesn't work */
        }

        void resolve_DidResolveService(NetService service)
        {
            selectedService = service;
        }

        private void servicesList_SelectedIndexChanged(object sender, EventArgs e)
        {
            NetService selected = null;
            try
            {
                selected = (NetService)servicesList.SelectedItems[0].Tag;
            }
            catch (Exception)
            {
                selected = null;
            }

            if (selected != null)
            {
                Resolve(selected);
            }
        }
    }
}