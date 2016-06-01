﻿USE [alvachiendb]
GO

/****** Object:  Table [dbo].[ENPOS]    Script Date: 5/31/2016 8:45:59 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ENPOS](
	[POSAbb] [nvarchar](10) NOT NULL,
	[POSName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_ENPOS] PRIMARY KEY CLUSTERED 
(
	[POSAbb] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[EnPOST]    Script Date: 5/31/2016 8:45:59 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnPOST](
	[POSAbb] [nvarchar](10) NOT NULL,
	[LangID] [nvarchar](5) NOT NULL,
	[POSName] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_EnPOST] PRIMARY KEY CLUSTERED 
(
	[POSAbb] ASC,
	[LangID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  View [dbo].[VEnPOS]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[VEnPOS]
AS
SELECT        dbo.ENPOS.*, dbo.EnPOST.LangID, dbo.EnPOST.POSName AS NativeName
FROM            dbo.ENPOS INNER JOIN
                         dbo.EnPOST ON dbo.ENPOS.POSAbb = dbo.EnPOST.POSAbb

GO
/****** Object:  Table [dbo].[EnWord]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnWord](
	[WordID] [int] IDENTITY(1,1) NOT NULL,
	[WordString] [nvarchar](100) NOT NULL,
	[Tags] [nvarchar](50) NULL,
 CONSTRAINT [PK_EnWords] PRIMARY KEY CLUSTERED 
(
	[WordID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[EnWordExplain]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnWordExplain](
	[WordID] [int] NOT NULL,
	[ExplainID] [int] NOT NULL,
	[POSID] [int] NOT NULL,
 CONSTRAINT [PK_EnWordExplain] PRIMARY KEY CLUSTERED 
(
	[WordID] ASC,
	[ExplainID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[EnWordExplainT]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnWordExplainT](
	[WordID] [int] NOT NULL,
	[ExplainID] [int] NOT NULL,
	[LangID] [nvarchar](5) NOT NULL,
	[ExplainString] [nvarchar](100) NOT NULL,
 CONSTRAINT [PK_EnWordExplainT] PRIMARY KEY CLUSTERED 
(
	[WordID] ASC,
	[ExplainID] ASC,
	[LangID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  View [dbo].[VEnWord]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[VEnWord]
AS
SELECT        dbo.EnWord.*, dbo.EnWordExplain.ExplainID, dbo.EnWordExplain.POSID, dbo.EnWordExplainT.LangID, dbo.EnWordExplainT.ExplainString
FROM            dbo.EnWord INNER JOIN
                         dbo.EnWordExplain ON dbo.EnWord.WordID = dbo.EnWordExplain.WordID INNER JOIN
                         dbo.EnWordExplainT ON dbo.EnWordExplain.WordID = dbo.EnWordExplainT.WordID AND dbo.EnWordExplain.ExplainID = dbo.EnWordExplainT.ExplainID

GO
/****** Object:  Table [dbo].[EnSentence]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnSentence](
	[SentenceID] [int] NOT NULL,
	[SentenceString] [nvarchar](200) NOT NULL,
	[Tags] [nvarchar](100) NULL,
 CONSTRAINT [PK_EnSentence] PRIMARY KEY CLUSTERED 
(
	[SentenceID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[EnSentenceExplain]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnSentenceExplain](
	[SentenceID] [int] NOT NULL,
	[ExplainID] [int] NOT NULL,
 CONSTRAINT [PK_EnSentenceExplain] PRIMARY KEY CLUSTERED 
(
	[SentenceID] ASC,
	[ExplainID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[EnSentenceExplainT]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnSentenceExplainT](
	[SentenceID] [int] NOT NULL,
	[ExplainID] [int] NOT NULL,
	[LangID] [nvarchar](5) NOT NULL,
	[ExplainString] [nvarchar](100) NOT NULL
) ON [PRIMARY]

GO
/****** Object:  View [dbo].[VEnSentence]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[VEnSentence]
AS
SELECT        dbo.EnSentence.*, dbo.EnSentenceExplain.ExplainID, dbo.EnSentenceExplainT.LangID, dbo.EnSentenceExplainT.ExplainString
FROM            dbo.EnSentence INNER JOIN
                         dbo.EnSentenceExplain ON dbo.EnSentence.SentenceID = dbo.EnSentenceExplain.SentenceID INNER JOIN
                         dbo.EnSentenceExplainT ON dbo.EnSentence.SentenceID = dbo.EnSentenceExplainT.SentenceID

GO
/****** Object:  Table [dbo].[EnSentenceWord]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EnSentenceWord](
	[SentenceID] [int] NOT NULL,
	[WordID] [int] NOT NULL,
 CONSTRAINT [PK_EnSentenceWord] PRIMARY KEY CLUSTERED 
(
	[SentenceID] ASC,
	[WordID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[TodoItem]    Script Date: 5/31/2016 8:46:00 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TodoItem](
	[ToDoID] [int] NOT NULL,
	[ItemName] [nvarchar](50) NOT NULL,
	[Priority] [int] NOT NULL,
	[Assignee] [nvarchar](50) NULL,
	[Dependence] [nvarchar](50) NULL,
	[StartDate] [datetime] NULL,
	[EndDate] [datetime] NULL,
	[ItemContent] [nvarchar](max) NULL,
	[Tags] [nvarchar](50) NULL,
 CONSTRAINT [PK_TodoItem] PRIMARY KEY CLUSTERED 
(
	[ToDoID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
ALTER TABLE [dbo].[EnPOST]  WITH CHECK ADD  CONSTRAINT [FK_EnPOST_POS] FOREIGN KEY([POSAbb])
REFERENCES [dbo].[ENPOS] ([POSAbb])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EnPOST] CHECK CONSTRAINT [FK_EnPOST_POS]
GO
ALTER TABLE [dbo].[EnSentenceWord]  WITH CHECK ADD  CONSTRAINT [FK_EnSentenceWord_Sentence] FOREIGN KEY([SentenceID])
REFERENCES [dbo].[EnSentence] ([SentenceID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EnSentenceWord] CHECK CONSTRAINT [FK_EnSentenceWord_Sentence]
GO
ALTER TABLE [dbo].[EnSentenceWord]  WITH CHECK ADD  CONSTRAINT [FK_EnSentenceWord_Word] FOREIGN KEY([WordID])
REFERENCES [dbo].[EnWord] ([WordID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EnSentenceWord] CHECK CONSTRAINT [FK_EnSentenceWord_Word]
GO
ALTER TABLE [dbo].[EnWordExplain]  WITH CHECK ADD  CONSTRAINT [FK_EnWordExplain_Word] FOREIGN KEY([WordID])
REFERENCES [dbo].[EnWord] ([WordID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EnWordExplain] CHECK CONSTRAINT [FK_EnWordExplain_Word]
GO
ALTER TABLE [dbo].[EnWordExplainT]  WITH CHECK ADD  CONSTRAINT [FK_EnWordExplainT_WordExplain] FOREIGN KEY([WordID], [ExplainID])
REFERENCES [dbo].[EnWordExplain] ([WordID], [ExplainID])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[EnWordExplainT] CHECK CONSTRAINT [FK_EnWordExplainT_WordExplain]
GO
