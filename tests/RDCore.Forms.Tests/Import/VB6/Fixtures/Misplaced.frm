VERSION 5.00
Begin VB.Form Misplaced
   Caption         =   "Misplaced"
   ClientHeight    =   3000
   ClientLeft      =   60
   ClientTop       =   345
   ClientWidth     =   4000
   LinkTopic       =   "Misplaced"
   ScaleHeight     =   3000
   ScaleWidth      =   4000
   Begin VB.ListBox lstHost
      Height          =   1500
      Left            =   120
      TabIndex        =   0
      Top             =   120
      Width           =   2000
      Begin VB.CommandButton cmdInList
         Caption         =   "In a list"
         Height          =   375
         Left            =   120
         TabIndex        =   1
         Top             =   120
         Width           =   1215
      End
   End
   Begin VB.Frame fraHost
      Caption         =   "Frame"
      Height          =   1500
      Left            =   2280
      TabIndex        =   2
      Top             =   120
      Width           =   1600
      Begin VB.ListBox lstInFrame
         Height          =   1000
         Left            =   120
         TabIndex        =   3
         Top             =   360
         Width           =   1300
         Begin VB.CommandButton cmdDeep
            Caption         =   "Deep"
            Height          =   375
            Left            =   60
            TabIndex        =   4
            Top             =   60
            Width           =   900
         End
      End
   End
   Begin VB.Timer tmrHost
      Interval        =   100
      Left            =   120
      Top             =   2400
      Begin VB.Label lblInTimer
         Caption         =   "Timer child"
         Height          =   255
         Left            =   600
         TabIndex        =   5
         Top             =   2400
         Width           =   1215
      End
   End
End
Attribute VB_Name = "Misplaced"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
