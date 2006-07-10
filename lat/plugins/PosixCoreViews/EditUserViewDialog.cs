// 
// lat - EditUserViewDialog.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
//

using Gtk;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Novell.Directory.Ldap;

namespace lat
{
	public class EditUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog editUserDialog;

		// General 
		[Glade.Widget] Gtk.Label usernameLabel;
		[Glade.Widget] Gtk.Label fullnameLabel;

		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry initialsEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;
		[Glade.Widget] Gtk.Entry officeEntry;

		[Glade.Widget] Gtk.Entry mailEntry;
		[Glade.Widget] Gtk.Entry phoneEntry;

		// Account
		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.SpinButton uidSpinButton;
		[Glade.Widget] Gtk.Entry homeDirEntry;
		[Glade.Widget] Gtk.Entry shellEntry;

		[Glade.Widget] Gtk.CheckButton smbEnableSambaButton;
		[Glade.Widget] Gtk.Entry smbLoginScriptEntry;
		[Glade.Widget] Gtk.Entry smbProfilePathEntry;
		[Glade.Widget] Gtk.Entry smbHomePathEntry;
		[Glade.Widget] Gtk.Entry smbHomeDriveEntry;
		[Glade.Widget] Gtk.Entry smbExpireEntry;
		[Glade.Widget] Gtk.Entry smbCanChangePwdEntry;
		[Glade.Widget] Gtk.Entry smbMustChangePwdEntry;
		[Glade.Widget] Gtk.Button smbSetExpireButton;
		[Glade.Widget] Gtk.Button smbSetCanButton;
		[Glade.Widget] Gtk.Button smbSetMustButton;

		// Groups
		[Glade.Widget] Gtk.Label primaryGroupLabel;
		[Glade.Widget] Gtk.TreeView memberOfTreeview;

		// Address
		[Glade.Widget] Gtk.TextView adStreetTextView;
		[Glade.Widget] Gtk.Entry adPOBoxEntry;
		[Glade.Widget] Gtk.Entry adCityEntry;
		[Glade.Widget] Gtk.Entry adStateEntry;
		[Glade.Widget] Gtk.Entry adZipEntry;

		// Telephones
		[Glade.Widget] Gtk.Entry tnHomeEntry;
		[Glade.Widget] Gtk.Entry tnPagerEntry;
		[Glade.Widget] Gtk.Entry tnMobileEntry;
		[Glade.Widget] Gtk.Entry tnFaxEntry;
		[Glade.Widget] Gtk.Entry tnIPPhoneEntry;

		// Organization
		[Glade.Widget] Gtk.Entry ozTitleEntry;
		[Glade.Widget] Gtk.Entry ozDeptEntry;
		[Glade.Widget] Gtk.Entry ozCompanyEntry;

		static string[] userAttrs = { "givenName", "sn", "initials", "cn",
			"uid", "uidNumber", "gidNumber", "userPassword", "mail", "loginShell", 
			"homeDirectory", "description", "physicalDeliveryOfficeName",
			"telephoneNumber", "postalAddress", "l", "st", "postalCode",
			"facsimileTelephoneNumber", "pager", "mobile", "homePhone", 
			"street", "title", "postOfficeBox" };

		static string[] sambaAttrs = { "sambaProfilePath", "sambaHomePath",
			"sambaHomeDrive", "sambaLogonScript", "sambaKickoffTime", 
			"sambaPwdCanChange", "sambaPwdMustChange" };

		bool _isSamba = false;
		bool firstTimeSamba = false;
		string _pass = "";
		string _smbLM = "";
		string _smbNT = "";
		string _smbSID = "";
		bool _passChanged = false;
		
		LdapEntry _le;
		Dictionary<string,string> _ui;

		List<LdapModification> _modList;

		Dictionary<string,LdapEntry> _allGroups;
		Dictionary<string,string> _allGroupGids;
		Dictionary<string,LdapModification> _modsGroup;
		Dictionary<string,string> _memberOfGroups;

		ListStore _memberOfStore;

		public EditUserViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null)
		{
			_le = le;
			_modList = new List<LdapModification> ();

			Init ();

			_isSamba = Util.CheckSamba (le);

			_ui = getUserInfo (le);

			getGroups (le);

			string userName = _ui["cn"];

			editUserDialog.Title = userName + " Properties";

			// General
			usernameLabel.UseMarkup = true;
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", _ui["uid"]);

			fullnameLabel.Text = String.Format ("{0} {1}", _ui["givenName"], _ui["sn"]);

			if (_ui.ContainsKey ("givenName"))
				firstNameEntry.Text = _ui["givenName"];
			
			if (_ui.ContainsKey ("initials"))
				initialsEntry.Text = _ui["initials"];
				
			if (_ui.ContainsKey ("sn"))
				lastNameEntry.Text = _ui["sn"];

			if (_ui.ContainsKey ("description"))				
				descriptionEntry.Text = _ui["description"];
			
			if (_ui.ContainsKey ("physicalDeliveryOfficeName"))
				officeEntry.Text = _ui["physicalDeliveryOfficeName"];
			
			if (_ui.ContainsKey ("mail"))
				mailEntry.Text = _ui["mail"];
				
			if (_ui.ContainsKey ("telephoneNumber"))				
				phoneEntry.Text = _ui["telephoneNumber"];

			// Account
			if (_ui.ContainsKey ("uid"))
				usernameEntry.Text = _ui["uid"];

			if (_ui.ContainsKey ("uidNumber"))				
				uidSpinButton.Value = int.Parse (_ui["uidNumber"]);
			
			if (_ui.ContainsKey ("loginShell"))
				shellEntry.Text = _ui["loginShell"];
				
			if (_ui.ContainsKey ("homeDirectory"))				
				homeDirEntry.Text = _ui["homeDirectory"];

			if (_isSamba) {
				toggleSambaWidgets (true);
				smbEnableSambaButton.Hide ();

				if (_ui.ContainsKey ("sambaLogonScript"))
					smbLoginScriptEntry.Text = _ui["sambaLogonScript"];
				
				if (_ui.ContainsKey ("sambaProfilePath"))
					smbProfilePathEntry.Text = _ui["sambaProfilePath"];
					
				if (_ui.ContainsKey ("sambaHomePath"))
					smbHomePathEntry.Text = _ui["sambaHomePath"];
				
				if (_ui.ContainsKey ("sambaHomeDrive"))
					smbHomeDriveEntry.Text = _ui["sambaHomeDrive"];
				
				if (_ui.ContainsKey ("sambaKickoffTime"))
					smbExpireEntry.Text = _ui["sambaKickoffTime"];
				
				if (_ui.ContainsKey ("sambaPwdCanChange"))
					smbCanChangePwdEntry.Text = _ui["sambaPwdCanChange"];
					
				if (_ui.ContainsKey ("sambaPwdMustChange"))
					smbMustChangePwdEntry.Text = _ui["sambaPwdMustChange"];

			} else {

				smbEnableSambaButton.Toggled += new EventHandler (OnSambaChanged);
				toggleSambaWidgets (false);
			}

			// Groups
			string pgid = _ui["gidNumber"];
			string pname = _allGroupGids [pgid];		
			primaryGroupLabel.Text = pname;			

			// Address
			if (_ui.ContainsKey ("street"))
				adStreetTextView.Buffer.Text = _ui["street"];
			
			if (_ui.ContainsKey ("postOfficeBox"))
				adPOBoxEntry.Text = _ui["postOfficeBox"];
			
			if (_ui.ContainsKey ("l"))
				adCityEntry.Text = _ui["l"];
				
			if (_ui.ContainsKey ("st"))
				adStateEntry.Text = _ui["st"];
				
			if (_ui.ContainsKey ("postalCode"))
				adZipEntry.Text = _ui["postalCode"];

			// Telephones
			if (_ui.ContainsKey ("homePhone"))
				tnHomeEntry.Text = _ui["homePhone"];
			
			if (_ui.ContainsKey ("pager"))
				tnPagerEntry.Text = _ui["pager"];
				
			if (_ui.ContainsKey ("mobile"))
				tnMobileEntry.Text = _ui["mobile"];
				
			if (_ui.ContainsKey ("facsimileTelephoneNumber"))				
				tnFaxEntry.Text = _ui["facsimileTelephoneNumber"];

			// Organization
			if (_ui.ContainsKey ("title"))
				ozTitleEntry.Text = _ui["title"];

			if (_ui.ContainsKey ("departmentNumber"))				
				ozDeptEntry.Text = _ui["departmentNumber"];
			
			if (_ui.ContainsKey ("o"))
				ozCompanyEntry.Text = _ui["o"];

			editUserDialog.Icon = Global.latIcon;
			editUserDialog.Run ();

			while (missingValues || errorOccured) {

				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				editUserDialog.Run ();				
			}

			editUserDialog.Destroy ();
		}
	
		void OnSambaChanged (object o, EventArgs args)
		{
			if (smbEnableSambaButton.Active) {

				_smbSID = server.GetLocalSID ();

				if (_smbSID == null) {
					Util.DisplaySambaSIDWarning (editUserDialog);
					smbEnableSambaButton.Active = false;
					return;
				}

				toggleSambaWidgets (true);
			} else {

				toggleSambaWidgets (false);
			}
		}

	    Dictionary<string,string> getUserInfo (LdapEntry le)
		{
			Dictionary<string,string> ui = new Dictionary<string,string> ();

			foreach (string a in userAttrs) {
				LdapAttribute attr;
				attr = le.getAttribute (a);

				if (attr == null)
					ui.Add (a, "");
				else
					ui.Add (a, attr.StringValue);
			}

			if (_isSamba) {
				foreach (string a in sambaAttrs) {
					LdapAttribute attr;
					attr = le.getAttribute (a);

					if (attr == null)
						ui.Add (a, "");
					else
						ui.Add (a, attr.StringValue);
				}
			} else {
				firstTimeSamba = true;
			}

			return ui;
		}

		bool checkMemberOf (string user, string[] members)
		{
			foreach (string s in members)
				if (s.Equals (user))
					return true;
	
			return false;			
		}

		void getGroups (LdapEntry le)
		{
			LdapEntry[] grps = server.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps) {

				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				if (le != null) {

					LdapAttribute a;
					a  = e.getAttribute ("memberUid");
					
					if (a != null) {

						if (checkMemberOf (_ui["uid"], a.StringValueArray)
						   && !_memberOfGroups.ContainsKey (nameAttr.StringValue)) {

							_memberOfGroups.Add (nameAttr.StringValue,"memeberUid");
							_memberOfStore.AppendValues (nameAttr.StringValue);
						}
					}
				}

				if (!_allGroups.ContainsKey (nameAttr.StringValue))
					_allGroups.Add (nameAttr.StringValue, e);

				if (!_allGroupGids.ContainsKey (nameAttr.StringValue))
					_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		void Init ()
		{
			_memberOfGroups = new Dictionary<string,string> ();
			_allGroups = new Dictionary<string,LdapEntry> ();
			_allGroupGids = new Dictionary<string,string> ();
			_modsGroup = new Dictionary<string,LdapModification> ();

			ui = new Glade.XML (null, "dialogs.glade", "editUserDialog", null);
			ui.Autoconnect (this);

			viewDialog = editUserDialog;

			TreeViewColumn col;

			_memberOfStore = new ListStore (typeof (string));
			memberOfTreeview.Model = _memberOfStore;
			memberOfTreeview.Selection.Mode = SelectionMode.Multiple;

			col = memberOfTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			_memberOfStore.SetSortColumnId (0, SortType.Ascending);
		}

		void toggleSambaWidgets (bool state)
		{
			if (state && firstTimeSamba) {
				string msg = Mono.Unix.Catalog.GetString (
					"You must reset the password for this account in order to set a samba password.");

				HIGMessageDialog dialog = new HIGMessageDialog (
						editUserDialog,
						0,
						Gtk.MessageType.Info,
						Gtk.ButtonsType.Ok,
						"Setting a samba password",
						msg);

				dialog.Run ();
				dialog.Destroy ();
			}

			smbLoginScriptEntry.Sensitive = state;
			smbProfilePathEntry.Sensitive = state;
			smbHomePathEntry.Sensitive = state;
			smbHomeDriveEntry.Sensitive = state;
			smbExpireEntry.Sensitive = state;
			smbCanChangePwdEntry.Sensitive = state;
			smbMustChangePwdEntry.Sensitive = state;
			smbSetExpireButton.Sensitive = state;
			smbSetCanButton.Sensitive = state;
			smbSetMustButton.Sensitive = state;
		}

		public void OnAddGroupClicked (object o, EventArgs args)
		{
			List<string> tmp = new List<string> ();
	
			foreach (KeyValuePair<string, LdapEntry> kvp in _allGroups) {
				if (kvp.Key == primaryGroupLabel.Text || _memberOfGroups.ContainsKey (kvp.Key))
					continue;

				tmp.Add (kvp.Key);
			}

			SelectGroupsDialog sgd = new SelectGroupsDialog (tmp.ToArray ());

			foreach (string name in sgd.SelectedGroupNames) {

				_memberOfStore.AppendValues (name);
		
				if (!_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Add (name, "memberUid");

				LdapAttribute attr = new LdapAttribute ("memberUid", _ui["uid"]);
				LdapModification lm = new LdapModification (LdapModification.ADD, attr);

				_modsGroup.Add (name, lm);

				updateGroupMembership ();

				_modsGroup.Clear ();
			}
		}

		public void OnRemoveGroupClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			TreePath[] tp = memberOfTreeview.Selection.GetSelectedRows (out model);

			for (int i  = tp.Length; i > 0; i--) {

				_memberOfStore.GetIter (out iter, tp[(i - 1)]);

				string name = (string) _memberOfStore.GetValue (iter, 0);

				_memberOfStore.Remove (ref iter);
		
				if (_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Remove (name);

				LdapAttribute attr = new LdapAttribute ("memberUid", _ui["uid"]);
				LdapModification lm = new LdapModification (LdapModification.DELETE, attr);

				_modsGroup.Add (name, lm);
			
				updateGroupMembership ();

				_modsGroup.Clear ();
			}
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", usernameEntry.Text);
			fullnameLabel.Text = String.Format ("{0} {1}", firstNameEntry.Text, lastNameEntry.Text);
			
		}

		public void OnPasswordClicked (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (pd.UnixPassword.Equals (""))
				return;

			_pass = pd.UnixPassword;
			_smbLM = pd.LMPassword;
			_smbNT = pd.NTPassword;

			_passChanged = true;
		}

		public void OnSetPrimaryGroupClicked (object o, EventArgs args)
		{
			List<string> tmp = new List<string> ();
	
			foreach (KeyValuePair<string, LdapEntry> kvp in _allGroups) {

				if (kvp.Key == primaryGroupLabel.Text)
					continue;

				tmp.Add (kvp.Key);
			}

			SelectGroupsDialog sgd = new SelectGroupsDialog (tmp.ToArray());

			if (sgd.SelectedGroupNames.Length > 0)
				primaryGroupLabel.Text = sgd.SelectedGroupNames[0];
		}

		public void OnSetExpireClicked (object o, EventArgs args)
		{
			TimeDateDialog td = new TimeDateDialog ();

			smbExpireEntry.Text = td.UnixTime.ToString ();
		}

		public void OnSetCanClicked (object o, EventArgs args)
		{
			TimeDateDialog td = new TimeDateDialog ();

			smbCanChangePwdEntry.Text = td.UnixTime.ToString ();
		}

		public void OnSetMustClicked (object o, EventArgs args)
		{
			TimeDateDialog td = new TimeDateDialog ();

			smbMustChangePwdEntry.Text = td.UnixTime.ToString ();
		}

		void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			try {
			
				server.Modify (groupEntry.DN, mods);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to modify group ") + groupEntry.DN;

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					editUserDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void updateGroupMembership ()
		{
			Log.Debug ("START updateGroupMembership ()");

			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [_modsGroup.Count];

			int count = 0;

			foreach (string key in _modsGroup.Keys) {

				Log.Debug ("group: {0}", key);

				LdapModification lm = (LdapModification) _modsGroup[key];
				groupEntry = (LdapEntry) _allGroups [key];

				mods[count] = lm;

				count++;
			}	

			modifyGroup (groupEntry, mods);

			Log.Debug ("END updateGroupMembership ()");
		}

		string getGidNumber (string name)
		{
			if (name == null)
				return null;

			LdapEntry le = (LdapEntry) _allGroups [name];		
			LdapAttribute attr = le.getAttribute ("gidNumber");

			if (attr != null)
				return attr.StringValue;
			
			return null;
		}

		Dictionary<string,string> getUpdatedUserInfo ()
		{
			Dictionary<string,string> retVal = new Dictionary<string,string> ();

			// General 
			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("initials", initialsEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", officeEntry.Text);
			retVal.Add ("mail", mailEntry.Text);
			retVal.Add ("telephoneNumber", phoneEntry.Text);

			// Account
			retVal.Add ("uid", usernameEntry.Text);
			retVal.Add ("uidNumber", uidSpinButton.Value.ToString());
			retVal.Add ("homeDirectory", homeDirEntry.Text);
			retVal.Add ("loginShell", shellEntry.Text);

			if (_passChanged)
				retVal.Add ("userPassword", _pass);

			if (_isSamba) {

				retVal.Add ("sambaProfilePath", smbProfilePathEntry.Text);
				retVal.Add ("sambaHomePath", smbHomePathEntry.Text);
				retVal.Add ("sambaHomeDrive", smbHomeDriveEntry.Text);
				retVal.Add ("sambaLogonScript", smbLoginScriptEntry.Text);

				if (smbExpireEntry.Text != "")
					retVal.Add ("sambaKickoffTime", smbExpireEntry.Text);

				if (smbCanChangePwdEntry.Text != "")
					retVal.Add ("sambaPwdCanChange", smbCanChangePwdEntry.Text);

				if (smbMustChangePwdEntry.Text != "")
					retVal.Add ("sambaPwdMustChange", smbMustChangePwdEntry.Text);
			}

			// Groups
			retVal.Add ("gidNumber", getGidNumber(primaryGroupLabel.Text));

			// Address
			retVal.Add ("street", adStreetTextView.Buffer.Text);
			retVal.Add ("l", adCityEntry.Text);
			retVal.Add ("st", adStateEntry.Text);
			retVal.Add ("postalCode", adZipEntry.Text);
			retVal.Add ("postOfficeBox", adPOBoxEntry.Text);

			// Telephones
			retVal.Add ("facsimileTelephoneNumber", tnFaxEntry.Text);
			retVal.Add ("pager", tnPagerEntry.Text);
			retVal.Add ("mobile", tnMobileEntry.Text);
			retVal.Add ("homePhone", tnHomeEntry.Text);
			retVal.Add ("ipPhone", tnIPPhoneEntry.Text);

			// Organization
			retVal.Add ("title", ozTitleEntry.Text);
			retVal.Add ("departmentNumber", ozDeptEntry.Text);
			retVal.Add ("o", ozCompanyEntry.Text);

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Dictionary<string,string> cui = getUpdatedUserInfo ();

			string[] objClass = {"posixaccount","inetorgperson", "person" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cui, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			_modList = getMods (userAttrs, _ui, cui);

			if (smbEnableSambaButton.Active) {

				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;

				List<LdapModification> smbMods = Util.CreateSambaMods (
							user_rid, 
							_smbSID,
							_smbLM,
							_smbNT);

				foreach (LdapModification l in smbMods)
					_modList.Add (l);
			
			} else if (_isSamba) {

				List<LdapModification> smbMods = getMods (sambaAttrs, _ui, cui);

				if (_passChanged) {

					LdapAttribute la; 
					LdapModification lm;

					la = new LdapAttribute ("sambaLMPassword", _smbLM);
					lm = new LdapModification (LdapModification.REPLACE, la);

					_modList.Add (lm);

					la = new LdapAttribute ("sambaNTPassword", _smbNT);
					lm = new LdapModification (LdapModification.REPLACE, la);

					_modList.Add (lm);
				}

				foreach (LdapModification l in smbMods)
					_modList.Add (l);
			}

			if (!Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true)) {
				errorOccured = true;
				return;
			}

			editUserDialog.HideAll ();
		}
	}
}
