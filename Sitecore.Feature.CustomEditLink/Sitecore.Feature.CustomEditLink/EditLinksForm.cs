using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Nexus.Consumption;
using Sitecore.Resources;
using Sitecore.SecurityModel;
using Sitecore.Shell;
using Sitecore.Shell.Applications.Dialogs.ItemLister;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web.UI;

namespace Sitecore.Feature.CustomEditLink
{
    public class EditLinksForm : DialogForm
    {
        /// <summary>
        /// The links.
        /// </summary>
        protected Scrollbox Links;

        public EditLinksForm()
        {
        }

        /// <summary>
        /// Builds the report.
        /// </summary>
        /// <param name="output">
        /// The output.
        /// </param>
        /// <param name="linkDatabase">
        /// The link database.
        /// </param>
        /// <param name="item">
        /// The Sitecore item.
        /// </param>
        private static void BuildReport(HtmlTextWriter output, LinkDatabase linkDatabase, Item item)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNull(linkDatabase, "linkDatabase");
            Assert.ArgumentNotNull(item, "item");
            bool queryString = WebUtil.GetQueryString("ignoreclones") == "1";
            ItemLink[] referrers = linkDatabase.GetReferrers(item);
            if (referrers.Length != 0)
            {
                ItemLink[] itemLinkArray = referrers;
                for (int i = 0; i < (int)itemLinkArray.Length; i++)
                {
                    ItemLink itemLink = itemLinkArray[i];
                    if (!queryString || !(itemLink.SourceFieldID == FieldIDs.SourceItem) && !(itemLink.SourceFieldID == FieldIDs.Source) && !(itemLink.SourceFieldID == FieldIDs.FinalLayoutField) && !(itemLink.SourceFieldID == FieldIDs.LayoutField))
                    {
                        Database database = Factory.GetDatabase(itemLink.SourceDatabaseName, false);
                        if (database != null)
                        {
                            Item item1 = database.Items[itemLink.SourceItemID];
                            if (item1 != null)
                            {
                                string str = string.Concat("L", ID.NewID.ToShortID());
                                output.Write(string.Concat("<div id=\"", str, "\" class=\"scLink\">"));
                                output.Write("<table class=\"scLinkTable\" cellpadding=\"0\" cellspacing=\"0\"><tr>");
                                output.Write("<td>");
                                ImageBuilder imageBuilder = new ImageBuilder()
                                {
                                    Src = ImageBuilder.ResizeImageSrc(item1.Appearance.Icon, 24, 24),
                                    Class = "scLinkIcon"
                                };
                                output.Write(imageBuilder.ToString());
                                output.Write("</td>");
                                output.Write("<td>");
                                output.Write("<div class=\"scLinkHeader\">");
                                output.Write(GetUIDisplayName(item1));
                                output.Write("</div>");
                                output.Write("<div class=\"scLinkDetails\">");
                                output.Write(item1.Paths.ContentPath);
                                output.Write("</div>");
                                EditLinksForm.WriteDivider(output);
                                output.Write("<div class=\"scLinkField\">");
                                output.Write(Translate.Text("Field:"));
                                output.Write(' ');
                                if (!itemLink.SourceFieldID.IsNull)
                                {
                                    Field field = item1.Fields[itemLink.SourceFieldID];
                                    if (field.GetTemplateField() == null)
                                    {
                                        output.Write(Translate.Text("[unknown field]"));
                                    }
                                    else
                                    {
                                        output.Write(field.DisplayName);
                                    }
                                }
                                else
                                {
                                    output.Write(Translate.Text("Template"));
                                }
                                output.Write("</div>");
                                output.Write("<div class=\"scLinkField\">");
                                output.Write(Translate.Text("Target:"));
                                output.Write(' ');
                                output.Write(item.Paths.ContentPath);
                                output.Write("</div>");
                                EditLinksForm.WriteDivider(output);
                                string str1 = string.Concat(new object[] { "(\"", itemLink.TargetDatabaseName, "\",\"", itemLink.TargetItemID, "\",\"", itemLink.TargetPath, "\",\"", itemLink.SourceDatabaseName, "\",\"", itemLink.SourceItemID, "\",\"", itemLink.SourceFieldID, "\",\"", str, "\")" });
                                string str2 = string.Concat("Remove", str1);
                                string str3 = string.Concat("Relink", str1);
                                EditLinksForm.WriteCommand(output, "Edit", "Applications/16x16/edit.png", string.Concat("Edit(\"", item1.ID, "\")"));
                                EditLinksForm.WriteCommand(output, "Remove Link", "Network/16x16/link_delete.png", str2);
                                EditLinksForm.WriteCommand(output, "Link to Other Item", "Network/16x16/link_new.png", str3);
                                output.Write("</td>");
                                output.Write("</tr></table>");
                                output.Write("</div>");
                            }
                        }
                    }
                }
            }
            foreach (Item child in item.Children)
            {
                EditLinksForm.BuildReport(output, linkDatabase, child);
            }
        }

        /// <summary>
        /// Builds the report.
        /// </summary>
        private void BuildReport()
        {
            HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
            ListString listStrings = new ListString(UrlHandle.Get()["list"]);
            UrlHandle.DisposeHandle(UrlHandle.Get());
            LinkDatabase linkDatabase = Globals.LinkDatabase;
            foreach (string listString in listStrings)
            {
                Assert.IsNotNull(Context.ContentDatabase, "content database");
                Item item = Context.ContentDatabase.Items[listString];
                Assert.IsNotNull(item, "item");
                EditLinksForm.BuildReport(htmlTextWriter, linkDatabase, item);
            }
            this.Links.InnerHtml = htmlTextWriter.InnerWriter.ToString();
        }

        /// <summary>
        /// Edits the specified database name.
        /// </summary>
        /// <param name="id">
        /// The item id.
        /// </param>
        protected void Edit(string id)
        {
            Assert.ArgumentNotNullOrEmpty(id, "id");
            UrlString urlString = new UrlString("/sitecore/shell/Applications/Content Manager/default.aspx");
            urlString.Append("fo", id);
            urlString.Append("mo", "popup");
            Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), "900", "560");
        }

        /// <summary>
        /// Handles a click on the Cancel button.
        /// </summary>
        /// <param name="sender">
        /// The event owner object.
        /// </param>
        /// <param name="args">
        /// The event arguments.
        /// </param>
        /// <remarks>
        /// When the user clicksCancel, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnCancel(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            Context.ClientPage.ClientResponse.SetDialogValue("no");
            base.OnCancel(sender, args);
        }

        /// <summary>
        /// Raises the load event.
        /// </summary>
        /// <param name="e">
        /// The <see cref="T:System.EventArgs" /> instance containing the event data.
        /// </param>
        /// <remarks>
        /// This method notifies the server control that it should perform actions common to each HTTP
        /// request for the page it is associated with, such as setting up a database query. At this
        /// stage in the page lifecycle, server controls in the hierarchy are created and initialized,
        /// view state is restored, and form controls reflect client-side data. Use the IsPostBack
        /// property to determine whether the page is being loaded in response to a client postback,
        /// or if it is being loaded and accessed for the first time.
        /// </remarks>
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            //Telemetry.TelemetryClient.Track(Telemetry.Links.EditLinksActivated, (ulong)1);
            //Telemetry.TelemetryClient.Track(Telemetry.Links.EditLinksOpened, (ulong)1);
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.BuildReport();
            }
        }

        /// <summary>
        /// Handles a click on the OK button.
        /// </summary>
        /// <param name="sender">
        /// The event owner.
        /// </param>
        /// <param name="args">
        /// The event arguments.
        /// </param>
        /// <remarks>
        /// When the user clicks OK, the dialog is closed by calling
        /// the <see cref="M:Sitecore.Web.UI.Sheer.ClientResponse.CloseWindow">CloseWindow</see> method.
        /// </remarks>
        protected override void OnOK(object sender, EventArgs args)
        {
            Assert.ArgumentNotNull(sender, "sender");
            Assert.ArgumentNotNull(args, "args");
            Context.ClientPage.ClientResponse.SetDialogValue("yes");
            base.OnOK(sender, args);
        }

        /// <summary>
        /// Removes the specified database name.
        /// </summary>
        /// <param name="targetDatabaseName">
        /// Name of the database.
        /// </param>
        /// <param name="targetItemID">
        /// The target item id.
        /// </param>
        /// <param name="targetPath">
        /// The target path.
        /// </param>
        /// <param name="sourceDatabaseName">
        /// Name of the source database.
        /// </param>
        /// <param name="sourceItemID">
        /// The source item ID.
        /// </param>
        /// <param name="sourceFieldID">
        /// The source field ID.
        /// </param>
        /// <param name="linkID">
        /// The link hashcode.
        /// </param>
        protected void Relink(string targetDatabaseName, string targetItemID, string targetPath, string sourceDatabaseName, string sourceItemID, string sourceFieldID, string linkID)
        {
            Assert.ArgumentNotNullOrEmpty(targetDatabaseName, "targetDatabaseName");
            Assert.ArgumentNotNullOrEmpty(targetItemID, "targetItemID");
            Assert.ArgumentNotNullOrEmpty(sourceDatabaseName, "sourceDatabaseName");
            Assert.ArgumentNotNullOrEmpty(sourceItemID, "sourceItemID");
            Assert.ArgumentNotNullOrEmpty(sourceFieldID, "sourceFieldID");
            Assert.ArgumentNotNullOrEmpty(linkID, "linkID");
            ClientPipelineArgs currentPipelineArgs = Context.ClientPage.CurrentPipelineArgs as ClientPipelineArgs;
            Assert.IsNotNull(currentPipelineArgs, typeof(ClientPipelineArgs));
            Database database = Factory.GetDatabase(targetDatabaseName);
            Assert.IsNotNull(database, typeof(Database), "Database: {0}", targetDatabaseName);
            Item item = database.GetItem(targetItemID);
            Assert.IsNotNull(item, typeof(Item), "ID: {0}", targetItemID);
            Item item1 = Factory.GetDatabase(sourceDatabaseName).Items[sourceItemID];
            if (item1 == null)
            {
                return;
            }
            if (!currentPipelineArgs.IsPostBack)
            {
                SelectItemOptions selectItemOption = new SelectItemOptions()
                {
                    Icon = "Network/16x16/link_new.png",
                    Title = "Relink",
                    Text = "Select the item that you want the link to point to. Then click Relink to set the link.",
                    ButtonText = "Relink",
                    SelectedItem = item
                };
                SheerResponse.ShowModalDialog(selectItemOption.ToUrlString().ToString(), true);
                currentPipelineArgs.WaitForPostBack();
            }
            else if (currentPipelineArgs.HasResult)
            {
                Assert.IsNotNull(Context.ContentDatabase, "content database");
                Item item2 = Context.ContentDatabase.GetItem(currentPipelineArgs.Result);
                Assert.IsNotNull(item2, typeof(Item), "Item \"{0}\" not found", currentPipelineArgs.Result);
                Item[] versions = item1.Versions.GetVersions(true);
                for (int i = 0; i < (int)versions.Length; i++)
                {
                    Item item3 = versions[i];
                    Field field = item3.Fields[sourceFieldID];
                    if (field != null)
                    {
                        CustomField customField = FieldTypeManager.GetField(field);
                        if (customField != null)
                        {
                            using (SecurityDisabler securityDisabler = new SecurityDisabler())
                            {
                                item3.Editing.BeginEdit();
                                ItemLink itemLink = new ItemLink(sourceDatabaseName, ID.Parse(sourceItemID), item3.Language, item3.Version, ID.Parse(sourceFieldID), targetDatabaseName, ID.Parse(targetItemID), Language.Invariant, Data.Version.Latest, targetPath);
                                customField.Relink(itemLink, item2);
                                item3.Editing.EndEdit();
                            }
                        }
                    }
                }
                SheerResponse.Remove(linkID);
                SheerResponse.Alert("The link has been changed.", Array.Empty<string>());
                return;
            }
        }

        /// <summary>
        /// Removes the specified database name.
        /// </summary>
        /// <param name="targetDatabaseName">
        /// Name of the database.
        /// </param>
        /// <param name="targetItemID">
        /// The target item id.
        /// </param>
        /// <param name="targetPath">
        /// The target path.
        /// </param>
        /// <param name="sourceDatabaseName">
        /// Name of the source database.
        /// </param>
        /// <param name="sourceItemID">
        /// The source item ID.
        /// </param>
        /// <param name="sourceFieldID">
        /// The source field ID.
        /// </param>
        /// <param name="linkID">
        /// The link hashcode.
        /// </param>
        protected void Remove(string targetDatabaseName, string targetItemID, string targetPath, string sourceDatabaseName, string sourceItemID, string sourceFieldID, string linkID)
        {
            Assert.ArgumentNotNullOrEmpty(targetDatabaseName, "targetDatabaseName");
            Assert.ArgumentNotNullOrEmpty(targetItemID, "targetItemID");
            Assert.ArgumentNotNullOrEmpty(sourceDatabaseName, "sourceDatabaseName");
            Assert.ArgumentNotNullOrEmpty(sourceItemID, "sourceItemID");
            Assert.ArgumentNotNullOrEmpty(sourceFieldID, "sourceFieldID");
            Assert.ArgumentNotNullOrEmpty(linkID, "linkID");
            Database database = Factory.GetDatabase(targetDatabaseName);
            Assert.IsNotNull(database, typeof(Database), "Database: {0}", targetDatabaseName);
            Item item = database.GetItem(targetItemID);
            Assert.IsNotNull(item, typeof(Item), "ID: {0}", targetItemID);
            Item item1 = Factory.GetDatabase(sourceDatabaseName).Items[sourceItemID];
            if (item1 == null)
            {
                return;
            }
            Item[] versions = item1.Versions.GetVersions(true);
            for (int i = 0; i < (int)versions.Length; i++)
            {
                Item item2 = versions[i];
                Field field = item2.Fields[sourceFieldID];
                if (field != null)
                {
                    CustomField customField = FieldTypeManager.GetField(field);
                    if (customField != null)
                    {
                        using (SecurityDisabler securityDisabler = new SecurityDisabler())
                        {
                            item2.Editing.BeginEdit();
                            ItemLink itemLink = new ItemLink(sourceDatabaseName, ID.Parse(sourceItemID), item2.Language, item2.Version, ID.Parse(sourceFieldID), targetDatabaseName, ID.Parse(targetItemID), Language.Invariant, Data.Version.Latest, targetPath);
                            customField.RemoveLink(itemLink);
                            item2.Editing.EndEdit();
                        }
                    }
                }
            }
            SheerResponse.Remove(linkID);
            SheerResponse.Alert("The link has been removed.", Array.Empty<string>());
        }

        /// <summary>
        /// Writes the command.
        /// </summary>
        /// <param name="output">
        /// The output.
        /// </param>
        /// <param name="header">
        /// The header.
        /// </param>
        /// <param name="icon">
        /// The icon path.
        /// </param>
        /// <param name="click">
        /// The click action.
        /// </param>
        private static void WriteCommand(HtmlTextWriter output, string header, string icon, string click)
        {
            Assert.ArgumentNotNull(output, "output");
            Assert.ArgumentNotNullOrEmpty(header, "header");
            Assert.ArgumentNotNullOrEmpty(icon, "icon");
            Assert.ArgumentNotNullOrEmpty(click, "click");
            output.Write(string.Concat("<span class=\"scLinkCommand scRollOver\" onmouseover=\"javascript:return scForm.rollOver(this, event)\" onfocus=\"javascript:return scForm.rollOver(this, event)\" onmouseout=\"javascript:return scForm.rollOver(this, event)\" onblur=\"javascript:return scForm.rollOver(this, event)\" onclick=\"", Context.ClientPage.GetClientEvent(click), "\">"));
            ImageBuilder imageBuilder = new ImageBuilder()
            {
                Src = icon,
                Class = "scLinkCommandIcon"
            };
            output.Write(imageBuilder.ToString());
            output.Write(Translate.Text(header));
            output.Write("</span>");
        }

        /// <summary>
        /// Writes the divider.
        /// </summary>
        /// <param name="output">
        /// The output.
        /// </param>
        private static void WriteDivider(HtmlTextWriter output)
        {
            Assert.ArgumentNotNull(output, "output");
            output.Write("<div class=\"scLinkDivider\">");
            output.Write(Images.GetSpacer(1, 1));
            output.Write("</div>");
        }
        public static string GetUIDisplayName(Item item)
        {
            if (UserOptions.View.UseDisplayName)
            {
                return item.DisplayName;
            }
            return item.Name;
        }

    }
}
