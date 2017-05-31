// JSON-Base64 test suite.  (http://jb64.org/)
// (C) 2014 CubicleSoft.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JB64
{
    class Program
    {
        static void Main(string[] args)
        {
            System.IO.StreamReader TempFile = new System.IO.StreamReader("../../tests.txt");
            string Data = TempFile.ReadToEnd();
            TempFile.Close();

            int x = 1;
            JArray Tests = JsonConvert.DeserializeObject<JArray>(Data);
            foreach (JObject Test in Tests)
            {
                Console.WriteLine("Test #" + x + ":  " + Test["comment"]);

                System.IO.StreamWriter TempFile2 = null;
                if (Test["check_records"] != null)  TempFile2 = new System.IO.StreamWriter("../../out.jb64");

                // Test the encoder.
                bool Passed = true;
                JB64Encode Encode = new JB64Encode();
                List<JB64Header> Headers = new List<JB64Header>();
                if (Test["headers"] != null)
                {
                    Passed = true;
                    foreach (JArray Header in Test["headers"])
                    {
                        try
                        {
                            Headers.Add(new JB64Header((string)Header[0], (string)Header[1]));
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Passed = false;
                        }
                    }

                    if (Passed)
                    {
                        try
                        {
                            string Line = Encode.EncodeHeaders(Headers);

                            if (Test["check_records"] != null)  TempFile2.Write(Line);
                        }
                        catch (JB64Exception)
                        {
                            Passed = false;
                        }
                    }

                    if (Passed == (bool)Test["header_result"])
                    {
                        Console.WriteLine("[PASS] Processed headers as expected.");
                    }
                    else
                    {
                        Console.WriteLine("[FAIL] Header processing returned the wrong result.");
                    }
                }

                if (Test["data"] != null)
                {
                    int x2 = 0;
                    foreach (JToken Row in Test["data"])
                    {
                        List<JB64Value> Values = new List<JB64Value>();
                        Passed = true;
                        foreach (JToken Col in Row)
                        {
                            JToken Col2 = Col;
                            if (Col2.Type == JTokenType.Property)  Col2 = ((JProperty)Col2).Value;

                            if (Col2.Type == JTokenType.Null)  Values.Add(new JB64Value());
                            else if (Col2.Type == JTokenType.Boolean)  Values.Add(new JB64Value((bool)Col2));
                            else if (Col2.Type == JTokenType.Integer)  Values.Add(new JB64Value((int)Col2));
                            else if (Col2.Type == JTokenType.Float)  Values.Add(new JB64Value((double)Col2));
                            else if (Col2.Type == JTokenType.String)  Values.Add(new JB64Value((string)Col2));
                            else  Passed = false;
                        }

                        if (Passed)
                        {
                            try
                            {
                                string Line = Encode.EncodeRecord(Values.ToArray());

                                if (Test["check_records"] != null)  TempFile2.Write(Line);
                            }
                            catch (JB64Exception)
                            {
                                Passed = false;
                            }

                            if (Passed == (bool)((JArray)Test["data_results"])[x2])
                            {
                                Console.WriteLine("[PASS] EncodeRecord() for record #" + (x2 + 1) + " returned expected result.");
                            }
                            else
                            {
                                Console.WriteLine("[FAIL] EncodeRecord() for record #" + (x2 + 1) + " returned unexpected result.");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[FAIL] Unexpected column type in test.");
                        }

                        x2++;
                    }
                }

                if (Test["check_records"] != null)
                {
                    TempFile2.Close();

                    // Test the decoder.
                    TempFile = new System.IO.StreamReader("../../out.jb64");
                    JB64Decode Decode = new JB64Decode();
                    string Line = TempFile.ReadLine();
                    try
                    {
                        List<JB64Header> Headers2 = Decode.DecodeHeaders(Line);

                        Console.WriteLine("[PASS] DecodeHeaders() succeeded.");

                        Passed = (Headers.Count == Headers2.Count);
                        if (Passed)
                        {
                            int x2;
                            for (x2 = 0; x2 < Headers.Count && Headers[x2].Name == Headers2[x2].Name && Headers[x2].Type == Headers2[x2].Type; x2++)
                            {
                            }

                            Passed = (x2 == Headers.Count);
                        }

                        if (Passed)
                        {
                            Console.WriteLine("[PASS] DecodeHeaders() returned expected result.");

                            if (Test["header_map"] != null)
                            {
                                try
                                {
                                    List<JB64HeaderMap> TempMap = new List<JB64HeaderMap>();
                                    foreach (JArray Col in Test["header_map"])
                                    {
                                        TempMap.Add(new JB64HeaderMap((string)Col[0], (string)Col[1], (bool)Col[2]));
                                    }
                                    Decode.SetHeaderMap(TempMap);

                                    Console.WriteLine("[PASS] SetHeaderMap() succeeded.");
                                }
                                catch (JB64Exception)
                                {
                                    Console.WriteLine("[FAIL] SetHeaderMap() failed.");
                                }
                            }

                            foreach (JArray Row in Test["check_records"])
                            {
                                Line = TempFile.ReadLine();
                                try
                                {
                                    JB64Value[] Result = Decode.DecodeRecord(Line);

                                    Console.WriteLine("[PASS] DecodeRecord() succeeded.");

                                    // Convert checking row to something easier to test.
                                    List<JB64Value> Values = new List<JB64Value>();
                                    Passed = true;
                                    foreach (JToken Col in Row)
                                    {
                                        JToken Col2 = Col;
                                        if (Col2.Type == JTokenType.Property)  Col2 = ((JProperty)Col2).Value;

                                        if (Col2.Type == JTokenType.Null)  Values.Add(new JB64Value());
                                        else if (Col2.Type == JTokenType.Boolean)  Values.Add(new JB64Value((bool)Col2));
                                        else if (Col2.Type == JTokenType.Integer)  Values.Add(new JB64Value((int)Col2));
                                        else if (Col2.Type == JTokenType.Float)  Values.Add(new JB64Value((double)Col2));
                                        else if (Col2.Type == JTokenType.String)  Values.Add(new JB64Value((string)Col2));
                                    }

                                    Passed = (Result.Length == Values.Count);
                                    if (Passed)
                                    {
                                        int x2;
                                        for (x2 = 0; x2 < Result.Length; x2++)
                                        {
                                            Values[x2].ConvertTo(Result[x2].Type);

                                            if (!Values[x2].Equals(Result[x2]))
                                            {
                                                Passed = false;
                                            }
                                        }
                                    }

                                    if (Passed)
                                    {
                                        Console.WriteLine("[PASS] DecodeRecord() returned matching record data.");
                                    }
                                    else
                                    {
                                        Console.WriteLine("[FAIL] DecodeRecord() returned record data that does not match.");
                                    }
                                }
                                catch (JB64Exception)
                                {
                                    Console.WriteLine("[FAIL] DecodeRecord() failed.");
                                }
                            }

                            Line = TempFile.ReadLine();
                            if (Line == null || Line == "")
                            {
                                Console.WriteLine("[PASS] File contained the correct amount of data.");
                            }
                            else
                            {
                                Console.WriteLine("[FAIL] File contained more data than expected.");
                            }

                        }
                        else
                        {
                            Console.WriteLine("[PASS] DecodeHeaders() returned unexpected result.");
                        }
                    }
                    catch (JB64Exception e)
                    {
                        Console.WriteLine("[FAIL] DecodeHeaders() failed (" + e.Message + ").");
                    }

                    TempFile.Close();
                }

                Console.WriteLine();
                x++;
            }

            Console.ReadKey();
        }
    }
}
