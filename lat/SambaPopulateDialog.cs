// 
// lat - SambaPopulateDialog.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
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
using Novell.Directory.Ldap;

namespace lat 
{
	public class SambaPopulateDialog
	{
		[Glade.Widget] Gtk.Dialog sambaPopulateDialog;
		[Glade.Widget] Gtk.HBox hbox451;
		[Glade.Widget] Gtk.Entry sidEntry;
		[Glade.Widget] Gtk.Entry domainEntry;
		[Glade.Widget] Gtk.Entry adminEntry;
		[Glade.Widget] Gtk.Entry guestEntry;
		[Glade.Widget] Gtk.Entry userOUEntry;
		[Glade.Widget] Gtk.Entry groupOUEntry;
		[Glade.Widget] Gtk.Entry computerOUEntry;
		[Glade.Widget] Gtk.Entry idmapOUEntry;
		
		Glade.XML ui;
		ComboBox serverComboBox;

		public SambaPopulateDialog ()
		{
			ui = new Glade.XML (null, "lat.glade", "sambaPopulateDialog", null);
			ui.Autoconnect (this);

			serverComboBox = Util.CreateServerCombo ();
			serverComboBox.Changed += OnServerChanged;
			hbox451.PackEnd (serverComboBox, true, true, 5);

			Connection conn = GetCurrentConnection ();
			
			userOUEntry.Text = "ou=Users," + conn.DirectoryRoot;
			groupOUEntry.Text = "ou=Groups," + conn.DirectoryRoot;
			computerOUEntry.Text = "ou=Computers," + conn.DirectoryRoot;
			idmapOUEntry.Text = "ou=Idmap," + conn.DirectoryRoot;			

			adminEntry.Text = "root";
			guestEntry.Text = "nobody";

			sambaPopulateDialog.Icon = Global.latIcon;
			sambaPopulateDialog.Run ();
			sambaPopulateDialog.Destroy ();
		}

		Connection GetCurrentConnection ()
		{
			TreeIter iter;
			if (!serverComboBox.GetActiveIter (out iter))
				return null;

			string name = (string) serverComboBox.Model.GetValue (iter, 0);
			
			Connection conn = Global.Connections [name];
			if (!conn.IsConnected)
				conn.Connect ();			
			
			return conn;
		}

		void OnServerChanged (object o, EventArgs args)
		{
			Connection conn = GetCurrentConnection ();
			
			userOUEntry.Text = "ou=Users," + conn.DirectoryRoot;
			groupOUEntry.Text = "ou=Groups," + conn.DirectoryRoot;
			computerOUEntry.Text = "ou=Computers," + conn.DirectoryRoot;
			idmapOUEntry.Text = "ou=Idmap," + conn.DirectoryRoot;			
		}

		void SelectContainer (string msg, string title, Gtk.Entry entry)
		{
			Connection conn = GetCurrentConnection ();
		
			SelectContainerDialog scd = new SelectContainerDialog (conn, sambaPopulateDialog);

			scd.Message = msg;
			scd.Title = title;
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
				entry.Text = scd.DN;
		}

		public void OnUserBrowseClicked (object o, EventArgs args)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString (
				"Where in the directory would\nyou like to store users?"));

			string title = 
				Mono.Unix.Catalog.GetString ("Select a user container");

			SelectContainer (msg, title, userOUEntry);
		}

		public void OnGroupBrowseClicked (object o, EventArgs args)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString (
				"Where in the directory would\nyou like to store groups?"));

			string title = Mono.Unix.Catalog.GetString ("Select a group container");

			SelectContainer (msg, title, groupOUEntry);
		}

		public void OnComputerBrowseClicked (object o, EventArgs args)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString (
				"Where in the directory would\nyou like to store computers?"));

			string title = Mono.Unix.Catalog.GetString ("Select a computer container");
			
			SelectContainer (msg, title, computerOUEntry);
		}

		public void OnIdmapBrowseClicked (object o, EventArgs args)
		{
			string msg = String.Format (
				Mono.Unix.Catalog.GetString (
				"Where in the directory would\nyou like to store the ID map?"));

			string title = Mono.Unix.Catalog.GetString ("Select an ID map container");

			SelectContainer (msg, title, idmapOUEntry);
		}
		
		static string getCN (string dn)
		{
			string [] split = dn.Split (new char[] {','});
			string cn = split[0].Remove (0, 3);

			return cn;
		}

		bool checkDN (string dn)
		{
			Connection conn = GetCurrentConnection ();
			LdapEntry le = conn.Data.GetEntry (dn);

			if (le == null)
				return false;

			return true;
		}

		void createOU (string dn)
		{
			LdapAttributeSet lset = new LdapAttributeSet ();
			lset.Add (new LdapAttribute ("objectclass", new string[] {"organizationalUnit"}));
			lset.Add (new LdapAttribute ("ou", getCN(dn)));
			
			LdapEntry entry = new LdapEntry (dn, lset);
			
			Connection conn = GetCurrentConnection ();
			Util.AddEntry (conn, entry);
		}

		void createUser (string dn, string name, string pass, string sid, 
					 string flags, string uid, string gid, 
					 string urid, string grid, string gecos)
		{
			LdapAttributeSet lset = new LdapAttributeSet ();
			lset.Add (new LdapAttribute ("objectclass", new string[] {"inetOrgPerson", "sambaSAMAccount", "posixAccount", "shadowAccount"}));
			lset.Add (new LdapAttribute ("cn", name));
			lset.Add (new LdapAttribute ("sn", name));
			lset.Add (new LdapAttribute ("gidNumber", gid));
			lset.Add (new LdapAttribute ("uid", name));
			lset.Add (new LdapAttribute ("uidNumber", uid));
			lset.Add (new LdapAttribute ("homeDirectory", "/dev/null"));
			lset.Add (new LdapAttribute ("sambaPwdLastSet", "0"));
			lset.Add (new LdapAttribute ("sambaLogonTime", "0"));
			lset.Add (new LdapAttribute ("sambaLogoffTime", "2147483647"));
			lset.Add (new LdapAttribute ("sambaKickoffTime", "2147483647"));
			lset.Add (new LdapAttribute ("sambaPwdCanChange", "0"));
			lset.Add (new LdapAttribute ("sambaPwdMustChange", "2147483647"));
			lset.Add (new LdapAttribute ("sambaPrimaryGroupSID", sid + "-" + grid));
			lset.Add (new LdapAttribute ("sambaLMPassword", pass));
			lset.Add (new LdapAttribute ("sambaNTPassword", pass));
			lset.Add (new LdapAttribute ("sambaAcctFlags", flags));
			lset.Add (new LdapAttribute ("sambaSID", sid + "-" + urid));
			lset.Add (new LdapAttribute ("loginShell", "/bin/false"));
			lset.Add (new LdapAttribute ("gecos", gecos));
		
			LdapEntry entry = new LdapEntry (dn, lset);
			Connection conn = GetCurrentConnection ();
			Util.AddEntry (conn, entry);
		}

		void createGroup (string dn, string gid, string desc, 
					  string sid, string grid, string gtype, 
					  string memberuid)
		{
			LdapAttributeSet lset = new LdapAttributeSet ();
			lset.Add (new LdapAttribute ("objectclass", new string[] {"posixGroup", "sambaGroupMapping"}));
			lset.Add (new LdapAttribute ("gidNumber", gid));
			lset.Add (new LdapAttribute ("cn", getCN(dn)));
			lset.Add (new LdapAttribute ("description", desc));
			lset.Add (new LdapAttribute ("sambaSID", sid + "-" + grid));
			lset.Add (new LdapAttribute ("sambaGroupType", gtype));
			lset.Add (new LdapAttribute ("displayName", getCN(dn)));
			
			if (memberuid != "")
				lset.Add (new LdapAttribute ("memberUid", memberuid));
	
			LdapEntry entry = new LdapEntry (dn, lset);
			Connection conn = GetCurrentConnection ();
			Util.AddEntry (conn, entry);
		}

		void createDomain (string dn, string domain, string sid)
		{
			LdapAttributeSet lset = new LdapAttributeSet ();
			lset.Add (new LdapAttribute ("objectclass", new string[] {"sambaDomain"}));
			lset.Add (new LdapAttribute ("sambaDomainName", domain));
			lset.Add (new LdapAttribute ("sambaSID", sid));
			
			LdapEntry entry = new LdapEntry (dn, lset);
			Connection conn = GetCurrentConnection ();
			Util.AddEntry (conn, entry);
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			// containers

			if (!checkDN (userOUEntry.Text))
				createOU (userOUEntry.Text);

			if (!checkDN (groupOUEntry.Text))
				createOU (groupOUEntry.Text);

			if (!checkDN (computerOUEntry.Text))
				createOU (computerOUEntry.Text);

			if (!checkDN (idmapOUEntry.Text))
				createOU (idmapOUEntry.Text);

			// users

			string dn = String.Format ("cn={0},{1}", adminEntry.Text, userOUEntry.Text);
			createUser (dn, adminEntry.Text, "XXX", sidEntry.Text, "[U          ]",  "0", "0", "500", "512", "Netbios Domain Administrator");

			dn = String.Format ("cn={0},{1}", guestEntry.Text, userOUEntry.Text);
			createUser (dn, guestEntry.Text, "NO PASSWORDXXXXXXXXXXXXXXXXXXXXX",  sidEntry.Text, "[NUD        ]", "999", "514", "2998", "514", "Netbios Domain Administrator");

			// groups

			dn = String.Format ("cn=Domain Admins,{0}", groupOUEntry.Text); 
			createGroup (dn, "512", "Netbios Domain Administrators", sidEntry.Text, "512", "2", adminEntry.Text);

			dn = String.Format ("cn=Domain Users,{0}", groupOUEntry.Text);
			createGroup (dn, "513", "Netbios Domain Users", sidEntry.Text, "513", "2", "");

			dn = String.Format ("cn=Domain Guests,{0}", groupOUEntry.Text);
			createGroup (dn, "514", "Netbios Domain Guests", sidEntry.Text, "514", "2", "");

			dn = String.Format ("cn=Domain Computers,{0}", groupOUEntry.Text);
			createGroup (dn, "515", "Netbios Domain Computers", sidEntry.Text, "515", "2", "");

			dn = String.Format ("cn=Administrators,{0}", groupOUEntry.Text);
			createGroup (dn, "544", "Netbios Domain Members can fully administer the computer/sambaDomainName", "S-1-5-32",  "544", "5", "");

			dn = String.Format ("cn=Account Operators,{0}", groupOUEntry.Text);
			createGroup (dn, "548", "Netbios Domain Users to manipulate users accounts", "S-1-5-32", "548", "5", "");

			dn = String.Format ("cn=Print Operators,{0}", groupOUEntry.Text);
			createGroup (dn, "550", "Netbios Domain Print Operators", "S-1-5-32", "550", "5", "");

			dn = String.Format ("cn=Backup Operators,{0}", groupOUEntry.Text);
			createGroup (dn, "551", "Netbios Domain Members can bypass file security to back up files", "S-1-5-32", "551", "5", "");

			dn = String.Format ("cn=Replicators,{0}", groupOUEntry.Text);
			createGroup (dn, "552", "Netbios Domain Supports file replication in a sambaDomainName", "S-1-5-32", "552", "5", "");

			Connection conn = GetCurrentConnection ();
			dn = String.Format ("sambaDomainName={0},{1}", domainEntry.Text, conn.DirectoryRoot);
			createDomain (dn, domainEntry.Text, sidEntry.Text);

			sambaPopulateDialog.HideAll ();
		}
	
		public void OnCancelClicked (object o, EventArgs args)
		{
			sambaPopulateDialog.HideAll ();
		}
	}
}
