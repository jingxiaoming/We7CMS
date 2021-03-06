using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using We7.CMS.Common.PF;
using We7.CMS.Common;
using We7.CMS.Accounts;



namespace We7.CMS.Web.Admin
{
    public partial class Permission_Func : BaseUserControl
    {
        protected MenuHelper MenuHelper
        {
            get { return HelperFactory.GetHelper<MenuHelper>(); }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                Initialize();
            }
        }

        string ownerType;
        public string OwnerType
        {
            set { ownerType = value; }
            get { return ownerType; }
        }

        string objectID;
        public string ObjectID
        {
            set { objectID = value; }
            get { return objectID; }
        }

        string ownerID;
        public string OwnerID
        {
            set { ownerID = value; }
            get { return ownerID; }
        }

        string entityID;
        public string EntityID
        {
            set { entityID = value; }
            get { return entityID; }
        }

        public int TabID
        {
            get
            {
                string tab = Request["tab"];
                if (tab != null && We7Helper.IsNumber(tab))
                    return int.Parse(tab);
                else
                    return 1;
            }
        }

        /// <summary>
        /// 菜单树HTML字符串
        /// </summary>
        public string MenuTreeHtml { get; set; }

        private void Initialize()
        {
            int typeID;

            if (OwnerType == "role")
            {
                if (TabID == 3)
                {
                    tipTitle.InnerText = "»角色可访问菜单设置";
                    tipValue.InnerText = "当前角色可以访问的菜单列表，请选择后保存即可！";
                }
                else
                {
                    tipTitle.InnerText = "»角色的功能权限设置";
                    tipValue.InnerText = "此处对当前对象进行角色的功能权限设置，请选择后保存即可！";
                }

                Role r = AccountHelper.GetRole(OwnerID);
                typeID = Constants.OwnerRole;
            }
            else
            {
                if (TabID == 3)
                {
                    tipTitle.InnerText = "»用户可访问菜单设置";
                    tipValue.InnerText = "当前用户可以访问的菜单列表，请选择后保存即可！";
                }
                else
                {
                    tipTitle.InnerText = "»用户的功能权限设置";
                    tipValue.InnerText = "此处对当前对象进行用户的功能权限设置，请选择后保存即可！";
                }

                Account act = AccountHelper.GetAccount(OwnerID, new string[] { "FirstName", "LastName", "CompanyState" });

                typeID = Constants.OwnerAccount;
            }

            List<Permission> ps = AccountHelper.GetPermissions(typeID, OwnerID, ObjectID);
            List<string> menuIds = new List<string>();
            foreach (Permission p in ps)
            {
                menuIds.Add(p.Content);



            }

            if (EntityID == "System.Channel")
            {
                ApplyPermissionToSubChannelsCheckBox.Visible = true;
            }

            MenuTreeHtml = BuildMenuTreeString(menuIds);
        }

        /// <summary>
        /// 创建菜单树Html
        /// </summary>
        /// <param name="menuIds"></param>
        /// <returns></returns>
        string BuildMenuTreeString(List<string> menuIds)
        {
            List<We7.CMS.Common.MenuItem> mytree = MenuHelper.GetMenuTree(We7Helper.EmptyGUID, EntityID);
            int i = MenuHelper.UpdatePermissionStateOfMenuTree(ref mytree, menuIds);
            return BuildSubmenuString(mytree);
        }

        /// <summary>
        /// 递归创建子菜单树
        /// </summary>
        /// <param name="mytree"></param>
        /// <returns></returns>
        private string BuildSubmenuString(List<We7.CMS.Common.MenuItem> mytree)
        {
            if (mytree == null || mytree.Count == 0)
                return "";
            else
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("<ul>");
                string strMenu =
    @"<li id=""{0}"" class=""{1} ""><a href=""#""  class=""{1} ""><ins>&nbsp;</ins>{2}</a>
    {3}
    </li>";

                foreach (We7.CMS.Common.MenuItem menu in mytree)
                {
                    string subMenu = BuildSubmenuString(menu.Items);
                    sb.Append(string.Format(strMenu, We7Helper.GUIDToFormatString(menu.ID), menu.PermissionState, menu.Title, subMenu));
                }
                sb.Append("</ul>");
                return sb.ToString();
            }
        }


        protected void SaveButton_Click(object sender, EventArgs e)
        {
            int typeID = OwnerType == "role" ? Constants.OwnerRole : Constants.OwnerAccount;

            string[] adds = null;
            //判断AddsTextBox.Text是否为空，其主要是由于前台JS所产生，为空则消除空；
            if (AddsTextBox.Text != "null" && AddsTextBox.Text != null && AddsTextBox.Text != string.Empty)
            {
                adds = AddsTextBox.Text.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
            }

            if (adds != null && adds.Length > 0)
            {
                for (int i = 0; i < adds.Length; i++)
                {
                    adds[i] = We7Helper.FormatToGUID(adds[i]);
                }
            }

            AccountHelper.DeletePermission(typeID, OwnerID, ObjectID);
            AccountHelper.AddPermission(typeID, OwnerID, ObjectID, adds);

            if (ApplyPermissionToSubChannelsCheckBox.Checked == true && ApplyPermissionToSubChannelsCheckBox.Visible)
            {
                List<Channel> subChannels = ChannelHelper.GetSubChannelList(ObjectID, true);

                foreach (Channel ch in subChannels)
                {
                    AccountHelper.DeletePermission(typeID, OwnerID, ch.ID);
                    AccountHelper.AddPermission(typeID, OwnerID, ch.ID, adds);
                }
            }

            if (OwnerType != "role")
            {
                Account act = AccountHelper.GetAccount(OwnerID, new string[] { "LoginName", "Email" });
                if (act != null)
                {
                    string userName = String.Format("{0}({1})", act.LoginName, act.Email);
                    string content = string.Format("给用户“{0}设置权限”", userName);
                    AddLog("权限设置", content);
                }
            }
            else
            {
                Role role = AccountHelper.GetRole(OwnerID);
                if (role != null)
                {
                    string content = string.Format("给角色“{0}设置权限”", role.Name);
                    AddLog("权限设置", content);
                }
            }
            Messages.ShowMessage("权限设置成功！");
            Initialize();
        }
    }
}

