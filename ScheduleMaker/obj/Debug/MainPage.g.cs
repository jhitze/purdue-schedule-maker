﻿#pragma checksum "C:\Users\Nate\Documents\Visual Studio 2012\Projects\Web\ScheduleMaker\ScheduleMaker\MainPage.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "8A07B0C9F56C56BE7E02F80AC41F2849"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18034
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using System.Windows.Shapes;
using System.Windows.Threading;


namespace ScheduleMaker {
    
    
    public partial class MainPage : System.Windows.Controls.UserControl {
        
        internal System.Windows.Controls.Grid LayoutRoot;
        
        internal System.Windows.Controls.TabControl tabBase;
        
        internal System.Windows.Controls.TabItem mainTab;
        
        internal System.Windows.Controls.TextBox courseInput;
        
        internal System.Windows.Controls.CheckBox chkExclude;
        
        internal System.Windows.Controls.CheckBox chkSmiles;
        
        internal System.Windows.Controls.ListBox classList;
        
        internal System.Windows.Controls.TextBox classInfoBox;
        
        internal System.Windows.Controls.Button btnAddToList;
        
        internal System.Windows.Controls.ListBox lstFinalClasses;
        
        internal System.Windows.Controls.TextBox txtFinalInfo;
        
        internal System.Windows.Controls.Button btnRemoveFromList;
        
        internal System.Windows.Controls.Button btnSubmit;
        
        internal System.Windows.Controls.ListBox lstExclude;
        
        internal System.Windows.Controls.Button btnExclude;
        
        internal System.Windows.Controls.TextBox txtExclude;
        
        internal System.Windows.Controls.Label lblExcludeInfo;
        
        internal System.Windows.Controls.ProgressBar progress;
        
        internal System.Windows.Controls.TabItem tabLayout;
        
        internal System.Windows.Controls.Grid grdSchedule;
        
        internal System.Windows.Controls.Label lblMonday;
        
        internal System.Windows.Controls.Label lblTuesday;
        
        internal System.Windows.Controls.Label lblWednesday;
        
        internal System.Windows.Controls.Label lblThursday;
        
        internal System.Windows.Controls.Label lblFriday;
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Windows.Application.LoadComponent(this, new System.Uri("/ScheduleMaker;component/MainPage.xaml", System.UriKind.Relative));
            this.LayoutRoot = ((System.Windows.Controls.Grid)(this.FindName("LayoutRoot")));
            this.tabBase = ((System.Windows.Controls.TabControl)(this.FindName("tabBase")));
            this.mainTab = ((System.Windows.Controls.TabItem)(this.FindName("mainTab")));
            this.courseInput = ((System.Windows.Controls.TextBox)(this.FindName("courseInput")));
            this.chkExclude = ((System.Windows.Controls.CheckBox)(this.FindName("chkExclude")));
            this.chkSmiles = ((System.Windows.Controls.CheckBox)(this.FindName("chkSmiles")));
            this.classList = ((System.Windows.Controls.ListBox)(this.FindName("classList")));
            this.classInfoBox = ((System.Windows.Controls.TextBox)(this.FindName("classInfoBox")));
            this.btnAddToList = ((System.Windows.Controls.Button)(this.FindName("btnAddToList")));
            this.lstFinalClasses = ((System.Windows.Controls.ListBox)(this.FindName("lstFinalClasses")));
            this.txtFinalInfo = ((System.Windows.Controls.TextBox)(this.FindName("txtFinalInfo")));
            this.btnRemoveFromList = ((System.Windows.Controls.Button)(this.FindName("btnRemoveFromList")));
            this.btnSubmit = ((System.Windows.Controls.Button)(this.FindName("btnSubmit")));
            this.lstExclude = ((System.Windows.Controls.ListBox)(this.FindName("lstExclude")));
            this.btnExclude = ((System.Windows.Controls.Button)(this.FindName("btnExclude")));
            this.txtExclude = ((System.Windows.Controls.TextBox)(this.FindName("txtExclude")));
            this.lblExcludeInfo = ((System.Windows.Controls.Label)(this.FindName("lblExcludeInfo")));
            this.progress = ((System.Windows.Controls.ProgressBar)(this.FindName("progress")));
            this.tabLayout = ((System.Windows.Controls.TabItem)(this.FindName("tabLayout")));
            this.grdSchedule = ((System.Windows.Controls.Grid)(this.FindName("grdSchedule")));
            this.lblMonday = ((System.Windows.Controls.Label)(this.FindName("lblMonday")));
            this.lblTuesday = ((System.Windows.Controls.Label)(this.FindName("lblTuesday")));
            this.lblWednesday = ((System.Windows.Controls.Label)(this.FindName("lblWednesday")));
            this.lblThursday = ((System.Windows.Controls.Label)(this.FindName("lblThursday")));
            this.lblFriday = ((System.Windows.Controls.Label)(this.FindName("lblFriday")));
        }
    }
}

