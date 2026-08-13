/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FalconWpf
{
    public class XmlManager
    {

        static public void SaveXml(string strName, object obj)
        {
            XmlDocument xml = new XmlDocument();
            XmlElement rootxel = xml.CreateElement("Root");
            XmlElement childxel = fn_GetXmlFromObject(xml, obj);
            rootxel.AppendChild(childxel);
            xml.AppendChild(rootxel);
            xml.Save(strName);
        }

        static private XmlElement fn_GetXmlFromObject(XmlDocument xml, object obj)
        {
            XmlElement xel = xml.CreateElement(obj.GetType().Name);
            List<PropertyInfo> list = new List<PropertyInfo>();
            list.AddRange(obj.GetType().GetProperties());
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].PropertyType.BaseType.Name == "IPropertyChanged")
                {
                    var tempClass = list[i].GetValue(obj);
                    XmlElement temp = fn_GetXmlFromObject(xml, tempClass);
                    xel.AppendChild(temp);
                }
                else if(list[i].PropertyType.Name == "DataTable")
                {
                    DataTable dtTable = list[i].GetValue(obj) as DataTable;

                    XmlElement temp = xml.CreateElement(list[i].PropertyType.Name);
                    
                    if(dtTable != null)
                    {
                        int nColCnt = dtTable.Columns.Count;
                        XmlAttribute attr = xml.CreateAttribute("Name");
                        attr.Value = list[i].Name;
                        temp.Attributes.Append(attr);
                        //temp.InnerText = list[i].Name;
                        //------------------------------------------------------
                        // Row 저장.
                        for (int j = 0; j < dtTable.Rows.Count; j++)
                        {
                            XmlElement rows = xml.CreateElement($"Rows{j}");
                            for (int k = 0; k < nColCnt; k++)
                            {
                                XmlElement value = xml.CreateElement(dtTable.Columns[k].ToString());
                                value.InnerText = dtTable.Rows[j][k].ToString();
                                rows.AppendChild(value);
                            }
                            temp.AppendChild(rows);
                        }

                        xel.AppendChild(temp);
                    }
                }
                else
                {
                    XmlElement temp = xml.CreateElement(list[i].Name);
                    temp.InnerText = list[i].GetValue(obj).ToString();
                    xel.AppendChild(temp);
                }
            }
            return xel;
        }

        static public bool LoadXml(string strName, object obj)
        {
            bool bRet = false;
            if (File.Exists(strName))
            {
                XmlDocument xml = new XmlDocument();

                xml.Load(strName);

                var root = xml.DocumentElement.ChildNodes[0];
                if (root != null)
                {
                    List<PropertyInfo> list = new List<PropertyInfo>();
                    list.AddRange(obj.GetType().GetProperties());

                    var recipe = root.ChildNodes;
                    for (int i = 0; i < recipe.Count; i++)
                    {
                        try
                        {
                            fn_GetObjectFromXml(recipe[i], list, obj);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    bRet = true;
                }
            }
            return bRet;
        }

        static private void fn_GetObjectFromXml(XmlNode xel, List<PropertyInfo> list, object obj)
        {
            
            for (int i = 0; i < list.Count; i++)
            {
                if (xel.ChildNodes.Count == 1)
                {
                    if (xel.Name == "DataTable")
                    {
                        //if (xel.FirstChild.InnerText == list[i].Name)
                        if (xel.Attributes["Name"].Value == list[i].Name)
                        {
                            DataTable dt = list[i].GetValue(obj) as DataTable;
                            dt.Columns.Clear();
                            dt.Rows.Clear();
                            for (int j = 0; j < xel.ChildNodes.Count; j++)
                            {
                                XmlNode node = xel.ChildNodes[j];
                                object[] obData = new object[node.ChildNodes.Count];
                                for (int k = 0; k < node.ChildNodes.Count; k++)
                                {
                                    if (node.Name == "Rows0")
                                        dt.Columns.Add(node.ChildNodes[k].Name);
                                    obData[k] = node.ChildNodes[k].InnerText;
                                }
                                dt.Rows.Add(obData);
                            }
                        }
                    }
                    else if (xel.Name == list[i].Name)
                    {
                        // Convert.ChangeType은 문자열 -> enum 변환을 지원하지 않으므로 별도 처리.
                        object tempValue = list[i].PropertyType.IsEnum
                            ? Enum.Parse(list[i].PropertyType, xel.InnerText)
                            : Convert.ChangeType(xel.InnerText, list[i].PropertyType);
                        list[i].SetValue(obj, tempValue);
                        break;
                    }
                }
                else
                {
                    if(xel.Name == "DataTable")
                    {
                        //if (xel.FirstChild.InnerText == list[i].Name)
                        if (xel.Attributes["Name"].Value == list[i].Name)
                        {
                            DataTable dt = list[i].GetValue(obj) as DataTable;
                            dt.Columns.Clear();
                            dt.Rows.Clear();
                            for (int j = 0; j < xel.ChildNodes.Count; j++)
                            {
                                XmlNode node = xel.ChildNodes[j];
                                object[] obData = new object[node.ChildNodes.Count];
                                for (int k = 0; k < node.ChildNodes.Count; k++)
                                {
                                    if (node.Name == "Rows0")
                                        dt.Columns.Add(node.ChildNodes[k].Name);
                                    obData[k] = node.ChildNodes[k].InnerText;
                                }
                                dt.Rows.Add(obData);
                            }
                        }
                    }
                    else if (xel.Name == list[i].PropertyType.Name)
                    {
                        for (int j = 0; j < xel.ChildNodes.Count; j++)
                        {
                            var tempClass = list[i].GetValue(obj);
                            List<PropertyInfo> listchild = new List<PropertyInfo>();
                            listchild.AddRange(tempClass.GetType().GetProperties());
                            fn_GetObjectFromXml(xel.ChildNodes[j], listchild, tempClass);
                        }
                    }
                }
            }
        }
    }
}
