VERSION 5.00
Begin VB.Form Intrinsics
   BackColor       =   &H00C0FFC0&
   Caption         =   "Intrinsic ""controls"""
   ClientHeight    =   6000
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   8000
   BeginProperty Font
      Name            =   "Arial"
      Size            =   9.75
      Charset         =   0
      Weight          =   700
      Underline       =   0   'False
      Italic          =   -1  'True
      Strikethrough   =   0   'False
   EndProperty
   KeyPreview      =   -1  'True
   LinkTopic       =   "Intrinsics"
   ScaleHeight     =   6000
   ScaleWidth      =   8000
   StartUpPosition =   2  'CenterScreen
   Tag             =   "form tag"
   Begin VB.Frame fraOptions
      Caption         =   "Options"
      Height          =   1500
      Left            =   120
      TabIndex        =   0
      Top             =   120
      Width           =   3000
      Begin VB.OptionButton optChoice
         Caption         =   "First"
         Height          =   255
         Index           =   0
         Left            =   120
         TabIndex        =   1
         Top             =   360
         Value           =   -1  'True
         Width           =   2000
      End
      Begin VB.OptionButton optChoice
         Caption         =   "Second"
         Height          =   255
         Index           =   1
         Left            =   120
         TabIndex        =   2
         Top             =   720
         Width           =   2000
      End
   End
   Begin VB.CommandButton cmdOK
      Caption         =   "&OK"
      Default         =   -1  'True
      BeginProperty Font
         Name            =   "Courier New"
         Size            =   12
         Charset         =   0
         Weight          =   400
         Underline       =   -1  'True
         Italic          =   0   'False
         Strikethrough   =   0   'False
      EndProperty
      Height          =   495
      Left            =   3360
      TabIndex        =   3
      ToolTipText     =   "Accept"
      Top             =   120
      Width           =   1215
   End
   Begin VB.CheckBox chkFlag
      Caption         =   "Flag"
      Enabled         =   0   'False
      Height          =   255
      Left            =   3360
      TabIndex        =   4
      Top             =   720
      Value           =   2  'Grayed
      Width           =   1215
   End
   Begin VB.TextBox txtNotes
      BackColor       =   &H80000018&
      ForeColor       =   &H000000FF&
      Height          =   855
      Left            =   120
      Locked          =   -1  'True
      MultiLine       =   -1  'True
      TabIndex        =   5
      Text            =   $"Intrinsics.frx":0000
      Top             =   1800
      Width           =   3000
   End
   Begin VB.ListBox lstItems
      Height          =   840
      ItemData        =   "Intrinsics.frx":001B
      Left            =   3360
      List            =   "Intrinsics.frx":002C
      MultiSelect     =   2  'Extended
      TabIndex        =   6
      Top             =   1200
      Width           =   1215
   End
   Begin VB.ComboBox cboStyle
      Height          =   315
      ItemData        =   "Intrinsics.frx":0044
      Left            =   4800
      List            =   "Intrinsics.frx":004E
      Style           =   2  'Dropdown List
      TabIndex        =   7
      Top             =   120
      Width           =   1455
   End
   Begin VB.PictureBox picLogo
      Appearance      =   0  'Flat
      Height          =   1215
      Left            =   4800
      Picture         =   "Intrinsics.frx":005C
      ScaleHeight     =   1185
      ScaleWidth      =   1425
      TabIndex        =   8
      Top             =   600
      Width           =   1455
      Begin VB.CommandButton cmdInside
         Caption         =   "In"
         Height          =   375
         Left            =   120
         TabIndex        =   9
         Top             =   120
         Width           =   615
      End
   End
   Begin VB.Label lblNote
      Alignment       =   2  'Center
      BackStyle       =   0  'Transparent
      BorderStyle     =   1  'Fixed Single
      Caption         =   "Intrinsics.frx":00AE
      Height          =   495
      Left            =   120
      TabIndex        =   10
      Top             =   2760
      UseMnemonic     =   0   'False
      Width           =   3000
   End
   Begin VB.Image imgEmpty
      Height          =   480
      Left            =   6480
      Picture         =   "Intrinsics.frx":00B9
      Stretch         =   -1  'True
      Top             =   120
      Width           =   480
   End
   Begin VB.Timer tmrTick
      Enabled         =   0   'False
      Interval        =   500
      Left            =   7440
      Top             =   5400
   End
   Begin VB.HScrollBar hsbValue
      Height          =   255
      LargeChange     =   3
      Left            =   120
      Max             =   10
      Min             =   1
      TabIndex        =   11
      Top             =   3360
      Value           =   5
      Width           =   3000
   End
   Begin VB.VScrollBar vsbValue
      Height          =   1215
      Left            =   6480
      Max             =   100
      SmallChange     =   5
      TabIndex        =   12
      Top             =   720
      Width           =   255
   End
   Begin VB.Shape shpBox
      BorderColor     =   &H000000FF&
      BorderWidth     =   3
      FillColor       =   &H0000FFFF&
      FillStyle       =   0  'Solid
      Height          =   735
      Left            =   3360
      Shape           =   4  'Rounded Rectangle
      Top             =   3360
      Width           =   1215
   End
   Begin VB.Line linDiagonal
      BorderColor     =   &H00FF0000&
      BorderWidth     =   2
      X1              =   120
      X2              =   3120
      Y1              =   4200
      Y2              =   5400
   End
   Begin VB.Menu mnuFile
      Caption         =   "&File"
      Begin VB.Menu mnuFileOpen
         Caption         =   "&Open..."
         Shortcut        =   ^O
      End
      Begin VB.Menu mnuFileSep
         Caption         =   "-"
      End
      Begin VB.Menu mnuFileExit
         Caption         =   "E&xit"
         Checked         =   -1  'True
      End
   End
   Begin VB.Menu mnuWindow
      Caption         =   "&Window"
      Enabled         =   0   'False
      WindowList      =   -1  'True
   End
End
Attribute VB_Name = "Intrinsics"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Private Sub cmdOK_Click()
    Unload Me
End Sub
