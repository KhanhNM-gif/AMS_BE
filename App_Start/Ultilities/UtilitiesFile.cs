using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;

/// <summary>
/// Summary description for UtilitiesFile
/// </summary>
public class UtilitiesFile
{
    public string FilePathVirtual { set; get; }
    public string FilePathPhysical { set; get; }

    public static bool CheckExtension(string fileName, string fileExtension)
    {
        FileInfo objFileInfo = new FileInfo(fileName);
        return objFileInfo.Extension.ToLower() == fileExtension.ToLower();
    }

    public static string GetExtension(string fileName)
    {
        FileInfo objFileInfo = new FileInfo(fileName);
        return objFileInfo.Extension.ToLower();
    }

    public static string ChangeExtension(string fileName, string fileExtensionOld, string fileExtensionNew)
    {
        if (!string.IsNullOrEmpty(fileName))
            return fileName.Replace(fileExtensionOld, fileExtensionNew);
        else
            return fileName;
    }

    public static string GetSizeName(long size)
    {
        if (size < 1024 * 1024)
            return String.Format("{0:0}", (double)size / 1024) + " KB";
        else
            return String.Format("{0:0}", (double)size / (1024 * 1024)) + " MB";
    }
   
    public static UtilitiesFile GetInfoFile(DateTime createDate, string fileNameAndExtension, string folderPathVirtual, bool isFileNameHasTime)
    {
        string folderPathPhysical = HttpContext.Current.Server.MapPath(folderPathVirtual);

        if (!isFileNameHasTime)
        {
            folderPathVirtual = folderPathVirtual + "/" + createDate.ToString("yyyy/MM/dd");
            folderPathPhysical = folderPathPhysical + "/" + createDate.ToString("yyyy/MM/dd");
        }
        else
        {
            folderPathVirtual = folderPathVirtual + "/" + createDate.ToString("yyyy/MM/dd") + "/" + createDate.ToString("HHmmss_ffffff");
            folderPathPhysical = folderPathPhysical + "/" + createDate.ToString("yyyy/MM/dd") + "/" + createDate.ToString("HHmmss_ffffff");
        }

        if (!Directory.Exists(folderPathVirtual))
            Directory.CreateDirectory(folderPathPhysical);
   
        UtilitiesFile objFileUpload = new UtilitiesFile();
        objFileUpload.FilePathVirtual = folderPathVirtual + "/" + fileNameAndExtension;
        objFileUpload.FilePathPhysical = folderPathPhysical + "/" + fileNameAndExtension;

        if (File.Exists(objFileUpload.FilePathPhysical)) File.Delete(objFileUpload.FilePathPhysical);

        return objFileUpload;
    }

    public static string FileToBase64(string filePathPhysical, out string fileContentBase64)
    {
        fileContentBase64 = "";
        try
        {
            System.IO.Stream stream = System.IO.File.OpenRead(filePathPhysical);
            byte[] fileBytes = new byte[stream.Length];
            int byteCount = stream.Read(fileBytes, 0, (int)stream.Length);
            fileContentBase64 = Convert.ToBase64String(fileBytes);
            stream.Close();
            return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }     
    }
    public static string StreamToBase64(System.IO.Stream stream, out string fileContentBase64)
    {
        fileContentBase64 = "";
        try
        {
            byte[] fileBytes = new byte[stream.Length];
            int byteCount = stream.Read(fileBytes, 0, (int)stream.Length);
            fileContentBase64 = Convert.ToBase64String(fileBytes);
            stream.Close();
            return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static string ContentBase64ToFile(string contentBase64, string filePathPhysical)
    {
        try
        {
            byte[] arrContent = Convert.FromBase64String(contentBase64);
            File.WriteAllBytes(filePathPhysical, arrContent);
            return "";
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    public static string GetStringFileContent(byte[] fileContent)
    {
        if (fileContent.Length == 0)
            return "";

        int index = 0;
        for (int i = fileContent.Length - 1; i >= 0; i--)
        {
            if (fileContent[i] > 0)
            {
                index = i + 1;
                break;
            }
        }
        return Encoding.UTF8.GetString(fileContent, 0, index);
    }

    public static string GetUrlPage()
    {
        if (HttpContext.Current == null) return "HttpContext.Current = null";
        if (HttpContext.Current.Request == null) return "HttpContext.Current.Request = null";
        if (HttpContext.Current.Request.Url == null) return "HttpContext.Current.Request.Url = null";
        if (HttpContext.Current.Request.Url.AbsoluteUri == null) return "HttpContext.Current.Request.Url.AbsoluteUri = null";

        String strPathAndQuery = HttpContext.Current.Request.Url.PathAndQuery;
        String strUrl = HttpContext.Current.Request.Url.AbsoluteUri.Replace(strPathAndQuery, "");
        return strUrl;
    }
}