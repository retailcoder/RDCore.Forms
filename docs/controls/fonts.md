# Fonts

A component holds its `Font` at the size VB6 draws it at, [`FormFont.AtDrawnSize`](../api/RDCore.Forms.Model.FormFont.yml), and that size is the one its `Size` reads back. A font from a form's file, from a designer and from code is held the same way. The sizes below were measured with VB6, native and p-code agreeing.

## Size

A font is drawn at a whole number of pixels, 96 to an inch, so its size is a multiple of 0.75 points.

|Font|Size drawn|
|---|---|
|A TrueType font, such as Arial or Tahoma|The size asked in whole pixels, rounded half up, and at least 2 pixels.|
|MS Sans Serif, MS Serif, Courier, System and Small Fonts|One of the bitmap font's own sizes, up to 160 pixels (120 points). Small Fonts is drawn at whole pixels from 154 pixels, as a TrueType font is.|

|Size asked (points)|Arial|MS Sans Serif|Courier|System|Small Fonts|
|---|---|---|---|---|---|
|8|8.25|8.25|9.75|9.75|8.25|
|10|9.75|9.75|9.75|9.75|8.25|
|14|14.25|13.5|12|9.75|13.5|
|24|24|24|24|19.5|24|
|66|66|67.5|60|68.25|66|

A bitmap font's size is not always the nearest one below the size asked, and does not always grow with it: GDI draws a size the font's file lacks by scaling one it has. MS Sans Serif asked for 107.25 points, 143 pixels, is drawn at 112.5 points, 150 pixels, and asked for 108 points is drawn at 108.

A font keeps the size it reads back, and a font given another name is drawn from that size. MS Sans Serif set to 14 points holds 13.5. Given the name Arial after that, it holds 13.5, as Arial set to 13.5 points does; Arial set to 14 points holds 14.25.

Past 160 pixels, a bitmap font is drawn at whole pixels. That is RDForms' own.

## A font never named

A `StdFont` a program makes and never names reads `MS Sans Serif` and is drawn in a scalable face, at whole pixels as a TrueType font is, except that a size of 2 pixels is drawn at 3 (2.25 points). [`FormFont.IsNamed`](../api/RDCore.Forms.Model.FormFont.yml) is False for it. It is drawn in the sans serif RDForms draws for a family that is not installed, with that face's line and column, never MS Sans Serif's. A font from a form's file is named.

## MS Sans Serif's line and column

MS Sans Serif's line height and [`Print`](drawing.md) column are VB6's at each size it is drawn at. An italic face's column is RDForms' own: the regular face's. Past 160 pixels, and for another font, they are RDForms' own, as [Drawing](drawing.md) describes.

|Sizes asked (pixels)|Size drawn (points)|Size drawn (pixels)|Line height (pixels)|Column (pixels)|Bold column (pixels)|
|---|---|---|---|---|---|
|1–12|8.25|11|13|5|6|
|13–15|9.75|13|16|7|8|
|16, 17|12|16|20|8|9|
|18, 19|13.5|18|24|9|10|
|20, 21|15|20|25|10|11|
|22|16.5|22|26|10|12|
|23|17.25|23|29|12|13|
|24, 25|18|24|29|12|13|
|26–29|19.5|26|32|14|16|
|30, 31|22.5|30|36|15|16|
|32–35|24|32|37|16|17|
|36–38|27|36|48|18|20|
|39|29.25|39|48|21|24|
|40–43|30|40|46|20|21|
|44, 45|33|44|52|20|24|
|46, 47|34.5|46|58|24|26|
|48–51|36|48|58|24|26|
|52, 53|39|52|64|28|32|
|54|40.5|54|72|27|30|
|55–59|41.25|55|65|25|30|
|60–63|45|60|72|30|32|
|64|48|64|74|32|34|
|65–68|48.75|65|80|35|40|
|69–71|51.75|69|87|36|39|
|72–77|54|72|87|36|39|
|78–87|60|80|92|40|42|
|88–91|67.5|90|108|45|48|
|92–95|69|92|116|48|52|
|96–99|72|96|111|48|51|
|100–107|75|100|125|50|55|
|108–110|81|108|144|45|50|
|111–119|86.25|115|145|60|65|
|120–126|90|120|138|60|63|
|127–137|96|128|148|64|68|
|138–142|103.5|138|174|60|65|
|144, 145|108|144|174|60|65|
|143, 146–156|112.5|150|180|75|80|
|157–160|120|160|184|80|84|

A size asked is the size in points times 4/3, rounded half up: 14 points, about 18.7 pixels, is asked as 19 and drawn at 18, with a column of 9 pixels and a line of 24.

---
[ACCUEIL](../index.fr.md) • [HOME](../index.md) | [BIENVENUE](../introduction.fr.md) • [WELCOME](../introduction.md) | [DÉMARRER](../getting-started.fr.md) • [GET STARTED](../getting-started.md) | [IMPORTER](../import/vb6.fr.md) • [IMPORT](../import/vb6.md) | [CONTROLS](index.md) | [FORMAT](../format/index.md) | 🌐 [rubberduckvba.ca](https://rubberduckvba.ca)
