// 
// lat - PosixUserViewPlugin.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
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

using System;
using Gtk;
using Gdk;
using Novell.Directory.Ldap;

namespace lat {

	public class PosixUserViewPlugin : ViewPlugin
	{
		public PosixUserViewPlugin () : base ()
		{
			config.ColumnAttributes =  new string[] { "uid", "cn" };
			config.ColumnNames = new string[] { "Username", "Real name" };
			config.Filter = "(&(objectclass=posixAccount)(objectclass=shadowAccount))";		
		}
	
		public override void Init ()
		{
		}

		public override void OnAddEntry (Connection conn)
		{
			new NewUserViewDialog (conn, this.DefaultNewContainer, this.PluginConfiguration.Defaults);
		}		

		public override void OnEditEntry (Connection conn, LdapEntry le)
		{
			new EditUserViewDialog (conn, le);
		}
					
		public override void OnPopupShow (Menu popup)
		{		
		}	

		public override void OnSetDefaultValues (Connection conn)
		{
			new UserDefaultValuesDialog (this, conn);
			Log.Debug ("this.PluginConfiguration.Defaults.Count: {0}", this.PluginConfiguration.Defaults.Count);
		}
			
		public override string[] Authors 
		{
			get {
				string[] cols = { "Loren Bandiera" };
				return cols;
			}
		}
		
		public override string Copyright 
		{ 
			get { return "MMG Security, Inc."; } 
		}
		
		public override string Description 
		{ 
			get { return "POSIX User View"; } 
		}
		
		public override string Name 
		{ 
			get { return "Users"; } 
		}
		
		public override string Version 
		{ 
			get { return Defines.VERSION; } 
		}

		public override string MenuLabel 
		{
			get { return "User"; }
		}

		public override AccelKey MenuKey 
		{
			get { return new AccelKey (Gdk.Key.Key_4, Gdk.ModifierType.ControlMask, AccelFlags.Visible); }
		}
		
		public override Gdk.Pixbuf Icon 
		{
			get { return Pixbuf.LoadFromResource ("stock_person.png"); }
		}
	}
}