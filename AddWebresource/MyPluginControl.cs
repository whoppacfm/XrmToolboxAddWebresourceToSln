using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using XrmToolBox.Extensibility;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk;
using McTools.Xrm.Connection;
using Microsoft.Crm.Sdk.Messages;

namespace AddWebresource
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;

        class WebresourceItem
        {
            public string name { get; set; }
            public Guid id { get; set; }
        }


        public MyPluginControl()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));
            
            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void tsbSample_Click(object sender, EventArgs e)
        {
            // The ExecuteMethod method handles connecting to an
            // organization if XrmToolBox is not yet connected
            ExecuteMethod(GetAccounts);
        }

        private void GetAccounts()
        {


        }

        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            listBox1.DisplayMember = "name";
            listBox1.ValueMember = "id";

            listBox1.Items.Clear();
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting webresources",
                Work = (worker, args) =>
                {
                    string fetchXml = @"<fetch version='1.0' top='50' output-format='xml-platform' mapping='logical' distinct='false' >
                        <entity name = 'webresource' >
                            <all-attributes/>
                            <filter type='and'>
                            <condition attribute='name' value='%";
                    fetchXml += textBox_search.Text;
                    fetchXml+=@"%' operator='like'/>
                            </filter>        
                            <order attribute='name' descending='false' />
                          </entity>
                        </fetch>";

                    /*
                        < filter type = 'or' >
                        < condition attribute = 'name' value = '%.js%' operator= 'like' />
                        < condition attribute = 'name' value = '%.html%' operator= 'like' />
                        </ filter >
                    */

                    FetchExpression fq = new FetchExpression(fetchXml);
                    args.Result = Service.RetrieveMultiple(fq);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    var result = args.Result as EntityCollection;
                    if (result != null)
                    {
                        //MessageBox.Show($"Found {result.Entities.Count} accounts");
                        foreach (var ent in result.Entities)
                        {
                            WebresourceItem item = new WebresourceItem();
                            item.name = ent.GetAttributeValue<string>("name");
                            item.id = ent.Id;
                            listBox1.Items.Add(item);
                        }
                    }
                }
            });
        }

        private void button_add_Click(object sender, EventArgs e)
        {
            if (textBox_solution.Text.Length > 1 && listBox1.SelectedIndex > -1)
            {
                var sln = textBox_solution.Text;
                Guid itemid = ((WebresourceItem)listBox1.Items[listBox1.SelectedIndex]).id;
                //MessageBox.Show(itemid);
                try
                {
                    AddSolutionComponentRequest req = new AddSolutionComponentRequest();
                    req.SolutionUniqueName = textBox_solution.Text;
                    req.ComponentId = itemid;
                    req.ComponentType = 61; //Webresource
                    req.AddRequiredComponents = true;
                    AddSolutionComponentResponse resp = (AddSolutionComponentResponse)Service.Execute(req);
                    MessageBox.Show("Success");
                }catch(Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }


            }
            else
            {
                MessageBox.Show("Please select a webresource from the list and insert the (unique) solution name");
            }
        }
    }
}