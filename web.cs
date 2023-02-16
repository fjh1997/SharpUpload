using System;
using System.IO;
using System.Net;
using System.Text;

class FileUploadSite

{

    private static String GetBoundary(String ctype)
{
    return "--" + ctype.Split(';')[1].Split('=')[1];
}

private static void SaveFile(Encoding enc, String boundary, Stream input)
{
    Byte[] boundaryBytes = enc.GetBytes(boundary);
    Int32 boundaryLen = boundaryBytes.Length;
    
    using (FileStream output = new FileStream("data", FileMode.Create, FileAccess.Write))
    {
        Byte[] buffer = new Byte[1024];
        Int32 len = input.Read(buffer, 0, 1024);
        Int32 startPos = -1;
        Int32 filenameStartPos = -1;
        Int32 filenameEndPos = -1;
        // Find start boundary
        while (true)
        {
            if (len == 0)
            {
                throw new Exception("Start Boundaray Not Found");
            }

            startPos = IndexOf(buffer, len, boundaryBytes);
            if (startPos >= 0)
            {
                break;
            }
            else
            {
                Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                len = input.Read(buffer, boundaryLen, 1024 - boundaryLen);
            }
        }
         String filename = "";
        // Skip four lines but read filename (Boundary, Content-Disposition, Content-Type, and a blank)
        for (Int32 i = 0; i < 4; i++)
        {
            while (true)
            {
                if (len == 0)
                {
                    throw new Exception("Preamble not Found.");
                }
                Byte[] filenameBytes=enc.GetBytes("filename=\"");
                filenameStartPos=IndexOf(buffer,buffer.Length,filenameBytes);
                if (filenameStartPos >= 0)
                {
                    filenameEndPos=IndexOf(buffer, enc.GetBytes("\""),filenameStartPos+filenameBytes.Length);

                    filename=enc.GetString(buffer,filenameStartPos+filenameBytes.Length,filenameEndPos-filenameStartPos-filenameBytes.Length);


                }
                startPos = Array.IndexOf(buffer, enc.GetBytes("\n")[0], startPos);
                if (startPos >= 0)
                {
                    startPos++;
                    break;
                }
                else
                {
                    len = input.Read(buffer, 0, 1024);
                }
            }
        }

        Array.Copy(buffer, startPos, buffer, 0, len - startPos);
        len = len - startPos;

        while (true)
        {
            Int32 endPos = IndexOf(buffer, len, boundaryBytes);
            if (endPos >= 0)
            {
                if (endPos > 0) output.Write(buffer, 0, endPos-2);
                break;
            }
            else if (len <= boundaryLen)
            {
                throw new Exception("End Boundaray Not Found");
            }
            else
            {
                output.Write(buffer, 0, len - boundaryLen);
                Array.Copy(buffer, len - boundaryLen, buffer, 0, boundaryLen);
                len = input.Read(buffer, boundaryLen, 1024 - boundaryLen) + boundaryLen;
            }
        }
    // Rename the file with the uploaded file name
        output.Flush();
        output.Close();
        File.Move("data", filename);
    }
}

private static Int32 IndexOf(Byte[] buffer, Int32 len, Byte[] boundaryBytes)
{
    for (Int32 i = 0; i <= len - boundaryBytes.Length; i++)
    {
        Boolean match = true;
        for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
        {
            match = buffer[i + j] == boundaryBytes[j];
        }

        if (match)
        {
            return i;
        }
    }

    return -1;
}
private static Int32 IndexOf(Byte[] buffer,  Byte[] boundaryBytes,Int32 startPos)
{
    for (Int32 i = startPos; i <= buffer.Length - boundaryBytes.Length; i++)
    {
        Boolean match = true;
        for (Int32 j = 0; j < boundaryBytes.Length && match; j++)
        {
            match = buffer[i + j] == boundaryBytes[j];
        }

        if (match)
        {
            return i;
        }
    }

    return -1;
}
    static void Main(string[] args)
    {
        // set listening host and port
        string url = "http://localhost:8080/";

        // begin lisening
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine("Listening at " + url);

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // handle uploading post
            if (request.HttpMethod == "POST")
            {
                // save files
                SaveFile(request.ContentEncoding, GetBoundary(request.ContentType), request.InputStream);


                // return success
                response.StatusCode = (int)HttpStatusCode.OK;
                string responseString = "File uploaded successfully.";
                byte[] responseBuffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = responseBuffer.Length;
                using (Stream output = response.OutputStream)
                {
                    output.Write(responseBuffer, 0, responseBuffer.Length);
                }
            }
            else
            {
                // return home page
                response.StatusCode = (int)HttpStatusCode.OK;
                string responseString = "<html><body><h1>File Upload Site</h1><form method=\"post\" action=\"upload\" enctype=\"multipart/form-data\"><input type=\"file\" name=\"file\"><input type=\"submit\" value=\"Upload\"></form></body></html>";
                byte[] responseBuffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = responseBuffer.Length;
                using (Stream output = response.OutputStream)
                {
                    output.Write(responseBuffer, 0, responseBuffer.Length);
                }
            }

            response.Close();
        }
    }
}