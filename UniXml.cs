using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

public class UniXml
{
    public static dynamic GetResponse(string url, string soapEnvelope, string xmlStart = "", string xmlEnd = "", string defaultRoot = "")
    {
        HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
        httpRequest.Method = "POST";
        httpRequest.ContentType = "text/xml; charset=utf-8";
        httpRequest.Credentials = CredentialCache.DefaultCredentials;
        //httpreq.Credentials = new NetworkCredential("UserName", "Password");

        Stream requestStream = httpRequest.GetRequestStream();
        StreamWriter strWriter = new StreamWriter(requestStream, Encoding.ASCII);
        strWriter.Write(soapEnvelope);
        strWriter.Close();

        HttpWebResponse httpWebResponse = (HttpWebResponse)httpRequest.GetResponse();
        StreamReader strReader = new StreamReader(httpWebResponse.GetResponseStream());
        string xmlResult = strReader.ReadToEnd();

        if (!string.IsNullOrEmpty(xmlStart) && !string.IsNullOrEmpty(xmlEnd))
        {
            var firstIndex = xmlResult.IndexOf(xmlStart);
            var lastIndex = xmlResult.LastIndexOf(xmlEnd);
            if (lastIndex > 0)
            {
                lastIndex = xmlResult.Length - lastIndex - xmlEnd.Length;
                xmlResult = xmlResult.Substring(firstIndex, xmlResult.Length - firstIndex - lastIndex);
            }
            else
                return null;
        }
        if (!string.IsNullOrEmpty(defaultRoot))
            xmlResult = string.Format("<{0}>{1}</{0}>", defaultRoot, xmlResult);

        var xmlDocument = XDocument.Parse(xmlResult);

        dynamic xmlContent = new ExpandoObject();
        ToDynamic(xmlContent, xmlDocument.Elements().First());
        return xmlContent;
    }
    public static void ToDynamic(dynamic parent, XElement node)
    {
        var item = new ExpandoObject();
        foreach (var attribute in node.Attributes())
            AddProperty(item, attribute.Name.ToString(), attribute.Value.Trim());
        if (node.HasElements)
        {
            var list = new List<dynamic>();
            foreach (var element in node.Elements())
                ToDynamic(item, element);
            AddProperty(parent, node.Name.ToString(), item);
        }
        else if (!node.HasAttributes)
            AddProperty(parent, node.Name.ToString(), node.Value);
        else
        {
            AddProperty(item, node.Name.ToString(), node.Value.Trim());
            AddProperty(parent, node.Name.ToString(), item);
        }
    }
    private static void AddProperty(dynamic parent, string name, object value)
    {
        if (parent is List<dynamic>)
            (parent as List<dynamic>).Add(value);
        else
        {
            var parentDict = (parent as IDictionary<String, object>);
            if (parentDict.ContainsKey(name) && parentDict[name] is List<dynamic>)
                (parentDict[name] as List<dynamic>).Add(value);
            else if (parentDict.ContainsKey(name))
            {
                var oldValue = parentDict[name];
                parentDict.Remove(name);
                parentDict[name] = new List<dynamic>();
                (parentDict[name] as List<dynamic>).Add(oldValue);
                (parentDict[name] as List<dynamic>).Add(value);
            }
            else
                parentDict[name] = value;
        }
    }
}