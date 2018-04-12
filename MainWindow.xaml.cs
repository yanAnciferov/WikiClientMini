using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;

namespace WikiParse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static string HTMLCode;
        public MainWindow()
        {
            InitializeComponent();
        }


        bool StartRequset(string req, string lang)
        {
            HTMLCode =
           getResponse($"https://" + lang + ".wikipedia.org/w/api.php?format=xml&action=query&prop=extracts&titles=" + req + "&redirects=true");

            Regex reg = new Regex("&lt;");
            HTMLCode = reg.Replace(HTMLCode, "<");
            reg = new Regex("&gt;");
            HTMLCode = reg.Replace(HTMLCode, ">");
            reg = new Regex("&quot;");
            HTMLCode = reg.Replace(HTMLCode, "\"");
            reg = new Regex("<br>");
            HTMLCode = reg.Replace(HTMLCode, "<br/>");

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(HTMLCode);

            var nodes = doc.DocumentElement.SelectNodes("/api/query/pages/page");
            if (nodes[0].Attributes["_idx"].Value == "-1")
                return false;

            string title = nodes[0].Attributes["title"].Value;
            nodes = doc.DocumentElement.SelectNodes("/api/query/pages/page/extract");

            var extract = nodes[0];

            var titleB = new Bold();
            titleB.Inlines.Add(title);
            titleB.FontSize = 30;
            var titleP = new Paragraph(titleB);
            page.Blocks.Add(titleP);
            page.Blocks.Add(new BlockUIContainer(new Separator()));
            Paragraph p = null;
            foreach (XmlNode item in extract)
            {
                if (item.Name == "p")
                {
                    p = new Paragraph();
                    foreach (XmlNode pIitem in item.ChildNodes)
                    {
                        if (pIitem.Name == "b")
                        {
                            var b = new Bold();
                            b.Inlines.Add(pIitem.InnerText);
                            p.Inlines.Add(b);
                        }
                        else if (pIitem.Name == "i")
                        {
                            var i = new Italic();
                            i.Inlines.Add(pIitem.InnerText);
                            p.Inlines.Add(i);
                        }
                        else if(pIitem.Name == "ul")
                        {
                            page.Blocks.Add(RenderList(pIitem));
                        }
                        else
                        {
                            p.Inlines.Add(pIitem.InnerText);
                        }
                    }
                    page.Blocks.Add(p);



                }else if (item.Name == "h2")
                {
                    p = new Paragraph();
                    var b = new Run();
                    b.Text = item.InnerText;
                    b.FontSize = 26;
                    p.Inlines.Add(b);
                    page.Blocks.Add(p);
                    page.Blocks.Add(new BlockUIContainer(new Separator()));
                }
                else if (item.Name == "h3")
                {
                    p = new Paragraph();
                    var b = new Bold();
                    b.Inlines.Add(item.InnerText);
                    b.FontSize = 18;
                    p.Inlines.Add(b);
                    page.Blocks.Add(p);
                }
                else if (item.Name == "ul")
                {
                    page.Blocks.Add(RenderList(item));
                }

            }

            return true;
        }

        List RenderList(XmlNode node)
        {

            List list = new List();

            foreach (XmlNode ulItem in node.ChildNodes)
            {
                if (ulItem.Name == "li")
                {
                    var listItem = new ListItem();
                    var p = new Paragraph();
                    listItem.Blocks.Add(p);
                    foreach (XmlNode liItem in ulItem.ChildNodes)
                    {
                        if (liItem.Name == "ul")
                        {
                            var ul = RenderList(liItem);
                            ul.Margin = new Thickness(0, 0, 0, 0);
                            listItem.Blocks.Add(ul);
                            list.ListItems.Add(listItem);
                        }
                        else if (liItem.Name == "b")
                        {
                            var b = new Bold();
                            b.Inlines.Add(liItem.InnerText);
                            p.Inlines.Add(b);
                        }
                        else if (liItem.Name == "i")
                        {
                            var i = new Italic();
                            i.Inlines.Add(liItem.InnerText);
                            p.Inlines.Add(i);
                        }
                        else
                        {
                            p.Inlines.Add(liItem.InnerText);
                        }
                       
                    }
                    list.ListItems.Add(listItem);

                }
                
            }

            return list;
        }

        Inline GetInline(XmlNode node)
        {
            return new Bold();
        }


        static string getResponse(string uri)
        {
            StringBuilder sb = new StringBuilder();
            byte[] buf = new byte[8192];
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.ContentType = @"text /html; UTF-8";
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
            int count = 0;
            do
            {
                count = resStream.Read(buf, 0, buf.Length);
                if (count != 0)
                {
                    sb.Append(Encoding.UTF8.GetString(buf, 0, count));
                }
            }
            while (count > 0);
            return sb.ToString();
        }



        private void TextBlock_KeyDown(object sender, KeyEventArgs e)
        {
            
            if (e.Key == Key.Enter)
            {
                searchPanel.Visibility = Visibility.Collapsed;
                DocReaderPanel.Visibility = Visibility.Visible;
                page.Blocks.Clear();

                string lang;

                if (search.Text.Any(wordByte => wordByte > 127))
                {
                    lang = "ru";
                }else
                {
                    lang = "en";
                }


                try
                {

                    if (StartRequset(search.Text, lang) == false)
                    {

                        var titleB = new Run();
                        titleB.Text = "Вы ввели не корректный запрос!";
                        titleB.FontSize = 30;
                        var titleP = new Paragraph(titleB);
                        page.Blocks.Add(titleP);
                    }
                }
                catch (Exception ex)
                {

                    var titleB = new Run();
                    titleB.Text = ex.Message;
                    titleB.FontSize = 30;
                    var titleP = new Paragraph(titleB);
                    page.Blocks.Add(titleP);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            searchPanel.Visibility = Visibility.Visible;
            DocReaderPanel.Visibility = Visibility.Collapsed;
        }
    }
}
