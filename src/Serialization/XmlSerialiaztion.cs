﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using DataServer;
/// <summary>
/// XmlSerializable  by IXmlSerializable
/// </summary>
namespace DataServer.Serialization
{
    public class XmlSerialiaztion
    {
        
        public static bool XmlSerial<T>(string sFilePath,T XmlSerialClass,ILog log) where T: IXmlSerializable
        {
            try
            {
                Stream sFileSteam = new FileStream(sFilePath, FileMode.Create, FileAccess.ReadWrite);
                XmlSerializer xmlserial = new XmlSerializer(typeof(T));
                xmlserial.Serialize(sFileSteam, XmlSerialClass);
                if(typeof(T)== typeof(Workbook))
                {
                    string sXml = "\r\n" + "<?mso-application progid=\"Excel.Sheet\"?>";
                    FileStreamInsert(sFileSteam, sXml, 21);
                }
                sFileSteam.Flush();
                sFileSteam.Close();
                return true;
            }
            catch(Exception ex)
            {
                MessageBox.Show(typeof(T).ToString() + ":" + ex.Message);
                return false;
            }

        }
        public static T XmlDeserial<T>(string sFilePath)
        {
            try
            {
                //T xmlDeserialClass = new T();
                Stream sFileSteam = new FileStream(sFilePath, FileMode.Open, FileAccess.ReadWrite);
                XmlSerializer xmlserial;
                if (typeof(T) == typeof(Workbook))
                {
                    xmlserial = new XmlSerializer(typeof(T), "urn:schemas-microsoft-com:office:spreadsheet");
                }
                else
                {
                    xmlserial = new XmlSerializer(typeof(T));
                }
                sFileSteam.Position = 0;
                T xmlDeserialClass = (T)xmlserial.Deserialize(sFileSteam);
                sFileSteam.Flush();
                sFileSteam.Close();
                return xmlDeserialClass;

            }
            catch (Exception ex)
            {
                MessageBox.Show(typeof(T).ToString()+ ":" + ex.Message);
                return default(T);
            }

        }
        /// <summary>
        /// File stream insert fuction
        /// </summary>
        /// <param name="sFileStream"></param>
        /// <param name="Insert string"></param>
        /// <param name="Insert Postion"></param>
        /// <param name="Data Block Size to transfer "></param>
        public static void FileStreamInsert(Stream sFileStream, string sInsert, long lPostion, int iBlockLength = 10000)
        {
            //string sInsertXml = "\r\n" + "<?mso-application progid=\"Excel.Sheet\"?>" + "\r\n";
            var insertChars = sInsert.ToCharArray();
            var insertBytes = new byte[insertChars.Length];
            Encoder e = Encoding.UTF8.GetEncoder();
            e.GetBytes(insertChars, 0, insertChars.Length, insertBytes, 0, true);
            //int iBlockLength = 10000;
            long lSurplusLength = sFileStream.Length - lPostion;
            for (long i = 0; i < lSurplusLength; i += iBlockLength)
            {
                int length = iBlockLength;
                if ((lSurplusLength - i) < iBlockLength)
                    length = (int)(lSurplusLength - i);
                var fileBytes = new byte[length];
                if (i == 0)//第一次移位增加长度，写Seek值变小（负数增大）
                {
                    sFileStream.Seek(-(i + length), SeekOrigin.End);
                    sFileStream.Read(fileBytes, 0, length);
                    sFileStream.Seek(-(i + length - insertBytes.Length), SeekOrigin.End);
                    sFileStream.Write(fileBytes, 0, length);
                }
                else //后面移位由于长度增加，读Seek值增大（负数变小）
                {
                    sFileStream.Seek(-(i + length + insertBytes.Length), SeekOrigin.End);
                    sFileStream.Read(fileBytes, 0, length);
                    sFileStream.Seek(-(i + length), SeekOrigin.End);
                    sFileStream.Write(fileBytes, 0, length);
                }
            }
            sFileStream.Seek(lPostion, SeekOrigin.Begin);
            sFileStream.Write(insertBytes, 0, insertBytes.Length);
        }
    }
}
