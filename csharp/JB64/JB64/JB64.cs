// JSON-Base64 reference implementation.  (http://jb64.org/)
// Public Domain.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace JB64
{
    /// <summary>
    /// Very basic exception handler for JSON-Base64.
    /// </summary>
    class JB64Exception : Exception
    {
        public JB64Exception()
        {
        }

        public JB64Exception(string message) : base(message)
        {
        }

        public JB64Exception(string message, Exception inner) : base(message, inner)
        {
        }
    }

    /// <summary>
    /// For declaring a single JSON-Base64 column name and type.
    /// </summary>
    class JB64Header
    {
        public string Name { get; private set; }
        public string Type { get; private set; }

        /// <summary>
        /// Declare a single JSON-Base64 column name and type.
        /// </summary>
        /// <param name="Name">Can be anything but null.</param>
        /// <param name="Type">May be one of 'boolean', 'integer', 'number', 'date', 'time', 'string', 'binary', or 'custom:...' as per the JSON-Base64 specification.</param>
        /// <exception cref="JB64Exception">Thrown when an invalid Type is specified.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when Name or Type is null.</exception>
        public JB64Header(string Name, string Type)
        {
            if (Name == null || Type == null)  throw new System.ArgumentNullException("Name and Type may not be null.");

            if (Type != "boolean" && Type != "integer" && Type != "number" && Type != "date" && Type != "time" && Type != "string" && Type != "binary" && (Type.Length < 7 || Type.Substring(0, 7) != "custom:"))
            {
                throw new JB64Exception("Invalid JSON-Base64 type specified.");
            }

            this.Name = Name;
            this.Type = Type;
        }
    }

    /// <summary>
    /// For declaring a single JSON-Base64 column name, type, and whether or not null values are allowed.
    /// Used for SetHeaderMap() calls in the decoder.
    /// </summary>
    class JB64HeaderMap
    {
        public string Name { get; private set; }
        public string Type { get; private set; }
        public bool NullAllowed { get; private set; }

        /// <summary>
        /// Declare a single JSON-Base64 column name, type, and null status mapping.
        /// </summary>
        /// <param name="Name">Can be anything but null.</param>
        /// <param name="Type">May be one of 'boolean', 'integer', 'number', 'date', 'time', 'string', 'binary', or 'custom:...' as per the JSON-Base64 specification.</param>
        /// <param name="NullAllowed">A boolean that specifies whether or not null values are allowed for this column.</param>
        /// <exception cref="JB64Exception">Thrown when an invalid Type is specified.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when Name or Type is null.</exception>
        public JB64HeaderMap(string Name, string Type, bool NullAllowed)
        {
            if (Name == null || Type == null)  throw new System.ArgumentNullException("Name and Type may not be null.");

            if (Type != "boolean" && Type != "integer" && Type != "number" && Type != "date" && Type != "time" && Type != "string" && Type != "binary" && (Type.Length < 7 || Type.Substring(0, 7) != "custom:"))
            {
                throw new JB64Exception("Invalid JSON-Base64 type specified.");
            }

            this.Name = Name;
            this.Type = Type;
            this.NullAllowed = NullAllowed;
        }
    }

    /// <summary>
    /// A container class for manipulating allowed JSON-Base64 data types.
    /// </summary>
    class JB64Value
    {
        /// <summary>
        /// A generic C# object whose type is determined by the Type variable.  Can be transformed with various ConvertTo...() methods.  May be set to null.
        /// </summary>
        public object Val { get; private set; }

        /// <summary>
        /// A string representing the current type of Val.  JSON-Base64 types only.
        /// </summary>
        public string Type { get; private set; }

        /// <summary>
        /// Sets the initial value to null.
        /// </summary>
        public JB64Value()
        {
            this.Val = null;
            this.Type = "string";
        }

        public JB64Value(bool Val)
        {
            this.Val = Val;
            this.Type = "boolean";
        }

        public JB64Value(int Val)
        {
            this.Val = Val;
            this.Type = "integer";
        }

        public JB64Value(double Val)
        {
            this.Val = Val;
            this.Type = "number";
        }

        public JB64Value(JB64Value Val)
        {
            this.Val = Val.Val;
            this.Type = Val.Type;
        }

        /// <summary>
        /// Constructor that supports the date, time, and string JSON-Base64 types.
        /// </summary>
        /// <param name="Val">A string.  May be null.</param>
        /// <param name="Type">May be one of 'date', 'time', or 'string'.</param>
        /// <exception cref="JB64Exception">Thrown when an invalid Type is specified.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when Type is null.</exception>
        public JB64Value(string Val, string Type = "string")
        {
            if (Type == null)  throw new System.ArgumentNullException("Type may not be null.");

            if (Type != "date" && Type != "time" && Type != "string")
            {
                throw new JB64Exception("Invalid JSON-Base64 type specified.");
            }

            if (Type == "date")  Val = FixDate(Val);
            if (Type == "time")  Val = FixTime(Val);
            this.Val = Val;
            this.Type = Type;
        }

        /// <summary>
        /// Constructor that supports the 'binary' and 'custom:...' JSON-Base64 types.
        /// </summary>
        /// <param name="Val">A byte[] array.  May be null.</param>
        /// <param name="Type">May be one of 'binary' or 'custom:...' (Where '...' is your custom type).</param>
        /// <exception cref="JB64Exception">Thrown when an invalid Type is specified.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when Type is null.</exception>
        public JB64Value(byte[] Val, string Type = "binary")
        {
            if (Type == null)  throw new System.ArgumentNullException("Type may not be null.");

            if (Type != "binary" && (Type.Length < 7 || Type.Substring(0, 7) != "custom:"))
            {
                throw new JB64Exception("Invalid JSON-Base64 type specified.");
            }

            this.Val = Val;
            this.Type = Type;
        }

        /// <summary>
        /// Converts the current value to the 'boolean' JSON-Base64 type (internally a 'bool').
        /// </summary>
        public void ConvertToBoolean()
        {
            if (this.Val != null)
            {
                try
                {
                    if (this.Type == "boolean")
                    {
                    }
                    else if (this.Type == "integer")
                    {
                        this.Val = ((int)this.Val != 0);
                    }
                    else if (this.Type == "number")
                    {
                        this.Val = ((double)this.Val != 0);
                    }
                    else if (this.Type == "date")
                    {
                        this.Val = ((string)this.Val != "0000-00-00 00:00:00");
                    }
                    else if (this.Type == "time")
                    {
                        this.Val = ((string)this.Val != "00:00:00");
                    }
                    else if (this.Type == "string")
                    {
                        this.Val = (Convert.ToInt32((string)this.Val) != 0);
                    }
                    else
                    {
                        this.Val = (Convert.ToInt32(Encoding.UTF8.GetString((byte[])this.Val)) != 0);
                    }
                }
                catch (Exception)
                {
                    this.Val = false;
                }
            }

            this.Type = "boolean";
        }

        /// <summary>
        /// Converts the current value to the 'integer' JSON-Base64 type (internally an 'int').
        /// </summary>
        public void ConvertToInteger()
        {
            if (this.Val != null)
            {
                try
                {
                    if (this.Type == "boolean")
                    {
                        this.Val = ((bool)this.Val ? 1 : 0);
                    }
                    else if (this.Type == "integer")
                    {
                    }
                    else if (this.Type == "number")
                    {
                        this.Val = (int)(double)this.Val;
                    }
                    else if (this.Type == "date")
                    {
                        this.Val = Convert.ToInt32(((string)this.Val).Remove(((string)this.Val).IndexOf('-')));
                    }
                    else if (this.Type == "time")
                    {
                        this.Val = Convert.ToInt32(((string)this.Val).Remove(((string)this.Val).IndexOf(':')));
                    }
                    else if (this.Type == "string")
                    {
                        this.Val = Convert.ToInt32((string)this.Val);
                    }
                    else
                    {
                        this.Val = Convert.ToInt32(Encoding.UTF8.GetString((byte[])this.Val));
                    }
                }
                catch (Exception)
                {
                    this.Val = 0;
                }
            }

            this.Type = "integer";
        }

        /// <summary>
        /// Converts the current value to the 'number' JSON-Base64 type (internally a 'double').
        /// </summary>
        public void ConvertToNumber()
        {
            if (this.Val != null)
            {
                try
                {
                    if (this.Type == "boolean")
                    {
                        this.Val = (double)((bool)this.Val ? 1 : 0);
                    }
                    else if (this.Type == "integer")
                    {
                        this.Val = (double)(int)this.Val;
                    }
                    else if (this.Type == "number")
                    {
                    }
                    else if (this.Type == "date")
                    {
                        this.Val = (double)Convert.ToInt32(((string)this.Val).Remove(((string)this.Val).IndexOf('-')));
                    }
                    else if (this.Type == "time")
                    {
                        this.Val = (double)Convert.ToInt32(((string)this.Val).Remove(((string)this.Val).IndexOf(':')));
                    }
                    else if (this.Type == "string")
                    {
                        this.Val = (double)Convert.ToInt32((string)this.Val);
                    }
                    else
                    {
                        this.Val = (double)Convert.ToInt32(Encoding.UTF8.GetString((byte[])this.Val));
                    }
                }
                catch (Exception)
                {
                    this.Val = (double)0;
                }
            }

            this.Type = "number";
        }

        /// <summary>
        /// Converts the current value to the 'date' JSON-Base64 type (internally a 'string').
        /// </summary>
        public void ConvertToDate()
        {
            if (this.Val != null)
            {
                try
                {
                    if (this.Type == "boolean")
                    {
                        this.Val = "0000-00-00 00:00:00";
                    }
                    else if (this.Type == "integer")
                    {
                        this.Val = FixDate(((int)this.Val).ToString());
                    }
                    else if (this.Type == "number")
                    {
                        this.Val = FixDate(((int)(double)this.Val).ToString());
                    }
                    else if (this.Type == "date")
                    {
                    }
                    else if (this.Type == "time")
                    {
                        this.Val = "0000-00-00 " + (string)this.Val;
                    }
                    else if (this.Type == "string")
                    {
                        this.Val = FixDate((string)this.Val);
                    }
                    else
                    {
                        this.Val = FixDate(Encoding.UTF8.GetString((byte[])this.Val));
                    }
                }
                catch (Exception)
                {
                    this.Val = "0000-00-00 00:00:00";
                }
            }

            this.Type = "date";
        }

        /// <summary>
        /// Converts the current value to the 'time' JSON-Base64 type (internally a 'string').
        /// </summary>
        public void ConvertToTime()
        {
            if (this.Val != null)
            {
                try
                {
                    if (this.Type == "boolean" || this.Type == "integer" || this.Type == "number")
                    {
                        this.Val = "00:00:00";
                    }
                    else if (this.Type == "date")
                    {
                        this.Val = FixTime(((string)this.Val).Substring(((string)this.Val).IndexOf(' ') + 1));
                    }
                    else if (this.Type == "time")
                    {
                    }
                    else if (this.Type == "string")
                    {
                        this.Val = FixTime((string)this.Val);
                    }
                    else
                    {
                        this.Val = FixTime(Encoding.UTF8.GetString((byte[])this.Val));
                    }
                }
                catch (Exception)
                {
                    this.Val = "00:00:00";
                }
            }

            this.Type = "time";
        }

        /// <summary>
        /// Converts the current value to the 'string' JSON-Base64 type (internally a 'string').
        /// </summary>
        public void ConvertToString()
        {
            if (this.Val != null)
            {
                try
                {
                    if (this.Type == "boolean")
                    {
                        this.Val = ((bool)this.Val ? 1 : 0).ToString();
                    }
                    else if (this.Type == "integer")
                    {
                        this.Val = ((int)this.Val).ToString();
                    }
                    else if (this.Type == "number")
                    {
                        this.Val = ((double)this.Val).ToString();
                    }
                    else if (this.Type == "date" || this.Type == "time" || this.Type == "string")
                    {
                    }
                    else
                    {
                        this.Val = Encoding.UTF8.GetString((byte[])this.Val);
                    }
                }
                catch (Exception)
                {
                    this.Val = "";
                }
            }

            this.Type = "string";
        }

        /// <summary>
        /// Converts the current value to the 'binary' or 'custom:...' JSON-Base64 type (internally a 'byte[]' array).
        /// </summary>
        /// <param name="NewType">One of 'binary' or 'custom:...' (Where '...' is your custom type). </param>
        /// <param name="BinaryMode">One of '', 'encode', or 'decode'.</param>
        /// <exception cref="JB64Exception">Thrown when an invalid NewType is specified.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when NewType or BinaryMode is null.</exception>
        public void ConvertToBinary(string NewType = "binary", string BinaryMode = "")
        {
            if (NewType == null || BinaryMode == null)  throw new System.ArgumentNullException("NewType and BinaryMode may not be null.");

            if (NewType != "binary" && (NewType.Length < 7 || NewType.Substring(0, 7) != "custom:"))
            {
                throw new JB64Exception("Invalid JSON-Base64 type specified.");
            }

            if (this.Val != null)
            {
                if (BinaryMode == "decode")  TryDecodeBinary();

                try
                {
                    if (this.Type == "boolean")
                    {
                        this.Val = Encoding.UTF8.GetBytes(((bool)this.Val ? 1 : 0).ToString());
                    }
                    else if (this.Type == "integer")
                    {
                        this.Val = Encoding.UTF8.GetBytes(((int)this.Val).ToString());
                    }
                    else if (this.Type == "number")
                    {
                        this.Val = Encoding.UTF8.GetBytes(((double)this.Val).ToString());
                    }
                    else if (this.Type == "date" || this.Type == "time" || this.Type == "string")
                    {
                        this.Val = Encoding.UTF8.GetBytes((string)this.Val);
                    }
                    else
                    {
                    }
                }
                catch (Exception)
                {
                    this.Val = Encoding.UTF8.GetBytes("");
                }
            }

            this.Type = NewType;

            if (this.Val != null && BinaryMode == "encode")  EncodeBinary();
        }

        /// <summary>
        /// A convenient way to convert the current value to another type.  Useful for simplifying loops to avoid a bunch of if-statements.
        /// </summary>
        /// <param name="Type">May be one of 'boolean', 'integer', 'number', 'date', 'time', 'string', 'binary', or 'custom:...' as per the JSON-Base64 specification.</param>
        /// <param name="BinaryMode">One of '', 'encode', or 'decode'.</param>
        public void ConvertTo(string Type, string BinaryMode = "")
        {
            switch (Type)
            {
                case "boolean":
                    ConvertToBoolean();
                    break;

                case "integer":
                    ConvertToInteger();
                    break;

                case "number":
                    ConvertToNumber();
                    break;

                case "date":
                    ConvertToDate();
                    break;

                case "time":
                    ConvertToTime();
                    break;

                case "string":
                    ConvertToString();
                    break;

                default:
                    ConvertToBinary(Type, BinaryMode);
                    break;
            }
        }

        /// <summary>
        /// Compares two JB64Value elements for equality.
        /// </summary>
        /// <param name="OtherVal">The JB64Value object to compare this one with.</param>
        /// <returns>True if equal, false if not.</returns>
        public bool Equals(JB64Value OtherVal)
        {
            if (this.Type != OtherVal.Type)  return false;
            if (this.Val == null && OtherVal.Val == null)  return true;
            if (this.Val == null || OtherVal.Val == null)  return false;

            switch (this.Type)
            {
                case "boolean":
                    return ((bool)this.Val == (bool)OtherVal.Val);

                case "integer":
                    return ((int)this.Val == (int)OtherVal.Val);

                case "number":
                    return ((double)this.Val == (double)OtherVal.Val);

                case "date":
                case "time":
                case "string":
                    return ((string)this.Val == (string)OtherVal.Val);

                default:
                    byte[] Val1 = (byte[])this.Val;
                    byte[] Val2 = (byte[])OtherVal.Val;

                    if (Val1.Length != Val2.Length)  return false;

                    int y = Val1.Length;
                    for (int x = 0; x < y; x++)
                    {
                        if (Val1[x] != Val2[x])  return false;
                    }

                    return true;
            }
        }

        /// <summary>
        /// Takes a null value and makes the value contain something sensible based on the Type.  Does nothing if the value is already not null.
        /// </summary>
        /// <param name="Type">May be one of 'boolean', 'integer', 'number', 'date', 'time', 'string', 'binary', or 'custom:...' as per the JSON-Base64 specification.</param>
        /// <exception cref="JB64Exception">Thrown when an invalid Type is specified.</exception>
        /// <exception cref="System.ArgumentNullException">Thrown when Type is null.</exception>
        public void ConvertToNotNull(string Type)
        {
            if (Type == null)  throw new System.ArgumentNullException("Type may not be null.");

            if (this.Val == null)
            {
                switch (Type)
                {
                    case "boolean":
                        this.Val = false;
                        break;

                    case "integer":
                        this.Val = 0;
                        break;

                    case "number":
                        this.Val = (double)0;
                        break;

                    case "date":
                        this.Val = "0000-00-00 00:00:00";
                        break;

                    case "time":
                        this.Val = "00:00:00";
                        break;

                    case "string":
                        this.Val = "";
                        break;

                    default:
                        if (Type != "binary" && (Type.Length < 7 || Type.Substring(0, 7) != "custom:"))
                        {
                            throw new JB64Exception("Invalid JSON-Base64 type specified.");
                        }

                        this.Val = new byte[0];
                        break;
                }

                this.Type = Type;
            }
        }

        /// <summary>
        /// Generally prefer calling ConvertTo() or ConvertToBinary() instead of this function.
        /// </summary>
        public void EncodeBinary()
        {
            if (this.Type != "binary" && (this.Type.Length < 7 || this.Type.Substring(0, 7) != "custom:"))
            {
                throw new JB64Exception("JSON-Base64 type is not binary.");
            }

            this.Val = Convert.ToBase64String((byte[])this.Val).Replace('+', '-').Replace('/', '_').Replace("=", "");
        }

        /// <summary>
        /// Generally prefer calling ConvertTo() or ConvertToBinary() instead of this function.
        /// </summary>
        public void TryDecodeBinary()
        {
            if (this.Type != "string")  return;

            try
            {
                string TempStr = ((string)this.Val).Replace('-', '+').Replace('_', '/');

                while (TempStr.Length % 4 != 0)  TempStr += '=';

                this.Val = Convert.FromBase64String(TempStr);
                this.Type = "binary";
            }
            catch (Exception)
            {
            }
        }

        private static string FixDate(string Date)
        {
            if (Date == null)  return null;

            Regex TempReg = new Regex("[^0-9]");
            Date = TempReg.Replace(Date, " ");
            TempReg = new Regex("\\s+");
            Date = TempReg.Replace(Date, " ");
            string[] Items = Date.Trim().Split(' ');

            Date = "";
            Date += (Items.Length > 0 ? Items[0] : "0000").PadLeft(4, '0');
            Date += "-";
            Date += (Items.Length > 1 ? Items[1] : "00").PadLeft(2, '0');
            Date += "-";
            Date += (Items.Length > 2 ? Items[2] : "00").PadLeft(2, '0');
            Date += " ";
            Date += (Items.Length > 3 ? Items[3] : "00").PadLeft(2, '0');
            Date += ":";
            Date += (Items.Length > 4 ? Items[4] : "00").PadLeft(2, '0');
            Date += ":";
            Date += (Items.Length > 5 ? Items[5] : "00").PadLeft(2, '0');

            return Date;
        }

        private static string FixTime(string Time)
        {
            if (Time == null)  return null;

            Regex TempReg = new Regex("[^0-9]");
            Time = TempReg.Replace(Time, " ");
            TempReg = new Regex("\\s+");
            Time = TempReg.Replace(Time, " ");
            string[] Items = Time.Trim().Split(' ');

            Time = "";
            Time += (Items.Length > 0 ? Items[0] : "00").PadLeft(2, '0');
            Time += ":";
            Time += (Items.Length > 1 ? Items[1] : "00").PadLeft(2, '0');
            Time += ":";
            Time += (Items.Length > 2 ? Items[2] : "00").PadLeft(2, '0');

            return Time;
        }
    }

    class JB64_Base
    {
        /// <summary>
        /// Internal function for computing a hex encoded MD5 hash.
        /// </summary>
        protected string GetMD5Hash(MD5 Hash, string Data)
        {
            byte[] DataText = Hash.ComputeHash(Encoding.UTF8.GetBytes(Data));

            StringBuilder Builder = new StringBuilder();

            for (int i = 0; i < DataText.Length; i++)
            {
                Builder.Append(DataText[i].ToString("x2"));
            }

            return Builder.ToString();
        }
    }

    class JB64Encode : JB64_Base
    {
        private List<JB64Header> Headers;
        private MD5 Hash;

        /// <summary>
        /// Basic constructor for the encoder.  Encode headers with the EncodeHeaders() method, then encode records with the EncodeRecord() method.
        /// </summary>
        public JB64Encode()
        {
            this.Headers = null;
        }

        /// <summary>
        /// Accepts a set of column information and then encodes the header line as per the JSON-Base64 spec.
        /// </summary>
        /// <param name="Headers">A List of JB64Header entries.  Each entry defines a column by which all records will be transformed.</param>
        /// <returns>A JSON-Base64 encoded string suitable for writing out to a binary streaming resource (e.g. file, network, etc).</returns>
        /// <exception cref="JB64Exception">Thrown if no headers are specified.</exception>
        public string EncodeHeaders(List<JB64Header> Headers)
        {
            if (Headers.Count < 1)  throw new JB64Exception("At least one JSON-Base64 header must be specified.");

            this.Headers = Headers;

            JArray Result = new JArray();
            foreach (JB64Header Header in this.Headers)
            {
                JArray Col = new JArray();
                Col.Add(new JValue(Header.Name));
                Col.Add(new JValue(Header.Type));

                Result.Add(Col);
            }

            Hash = MD5.Create();

            return EncodeLine(Result);
        }

        /// <summary>
        /// Accepts a set of data and then encodes it using the header information for each column as per the JSON-Base64 spec.
        /// </summary>
        /// <param name="Data">An array of JB64Value entries.  Must have the same number of columns as the header.</param>
        /// <returns>A JSON-Base64 encoded string suitable for writing out to a binary streaming resource (e.g. file, network, etc).</returns>
        /// <exception cref="JB64Exception">Thrown if headers haven't been encoded first or the number of Data elements don't match the number of header elements.</exception>
        public string EncodeRecord(JB64Value[] Data)
        {
            if (this.Headers == null)  throw new JB64Exception("JSON-Base64 headers must be encoded before record data can be encoded.");
            if (Data.Length != this.Headers.Count)  throw new JB64Exception("The number of data elements must match the number of headers.");

            // Normalize the input.
            JArray Result = new JArray();
            int x = 0;
            foreach (JB64Header Header in this.Headers)
            {
                Data[x].ConvertTo(Header.Type, "encode");

                Result.Add(Data[x].Val);
                x++;
            }

            return EncodeLine(Result);
        }

        protected string EncodeLine(JArray Line)
        {
            string Result = JsonConvert.SerializeObject(Line);
            Result = Base64Encode(Result).Replace('+', '-').Replace('/', '_').Replace("=", "");
            Result += "." + GetMD5Hash(Hash, Result);
            Result += "\r\n";

            return Result;
        }

        private string Base64Encode(string Text)
        {
            var TextBytes = Encoding.UTF8.GetBytes(Text);

            return Convert.ToBase64String(TextBytes);
        }
    }

    class JB64Decode : JB64_Base
    {
        private List<JB64Header> Headers;
        private List<JB64HeaderMap> Map;
        private MD5 Hash;

        /// <summary>
        /// Basic constructor for the decoder.  Decode headers with the DecodeHeaders() method or set custom headers with the SetHeaders() method, then optionally set a header map with SetHeaderMap() to automatically transform values to your application's needs, then decode records with the DecodeRecord() method.
        /// </summary>
        public JB64Decode()
        {
            this.Headers = null;
        }

        /// <summary>
        /// Accepts a JSON-Base64 encoded string as input and returns the decoded headers.
        /// </summary>
        /// <param name="Line">A line of JSON-Base64 encoded input.</param>
        /// <returns>A list of decoded headers.</returns>
        /// <exception cref="JB64Exception">Thrown if headers are unable to be decoded.</exception>
        public List<JB64Header> DecodeHeaders(string Line)
        {
            this.Headers = null;
            this.Map = null;

            Hash = MD5.Create();

            JArray Headers = DecodeLine(Line);

            return SetHeaders(Headers);
        }

        /// <summary>
        /// Accepts a set of column information to use as the headers.
        /// </summary>
        /// <param name="Headers">A List of JB64Header entries.  Each entry defines a column by which all records will be transformed.</param>
        /// <exception cref="JB64Exception">Thrown if no headers are defined.</exception>
        public void SetHeaders(List<JB64Header> Headers)
        {
            this.Headers = null;
            this.Map = null;

            if (Headers.Count < 1)  throw new JB64Exception("At least one JSON-Base64 header must be specified.");

            Hash = MD5.Create();

            this.Headers = Headers;
        }

        /// <summary>
        /// Accepts a JArray (Json.NET) as input and attempts to convert to a list of JB64Header entries.  Other SetHeaders() calls tend to be easier to use.
        /// </summary>
        /// <param name="Headers">A JArray (from NuGet Json.NET).</param>
        /// <returns>A List of JB64Header entries.</returns>
        /// <exception cref="JB64Exception">Thrown if headers are unable to be decoded.</exception>
        public List<JB64Header> SetHeaders(JArray Headers)
        {
            this.Headers = null;
            this.Map = null;

            if (Headers.Count < 1)  throw new JB64Exception("At least one JSON-Base64 header must be specified.");

            int x = 1;
            List<JB64Header> Headers2 = new List<JB64Header>();
            foreach (JArray Col in Headers)
            {
                if (Col.Count != 2 || Col[0].Type != JTokenType.String || Col[1].Type != JTokenType.String)  throw new JB64Exception("JSON-Base64 header " + x + " is not an array with two string entries.");

                Headers2.Add(new JB64Header((string)Col[0], (string)Col[1]));

                x++;
            }

            this.Headers = Headers2;

            return this.Headers;
        }

        /// <summary>
        /// Sets the post header transformation record map.  The headers are used to make sure the data is normalized to what was declared.  This is a post-normalization method to automatically transform values to your application's needs.
        /// </summary>
        /// <param name="Map">A List of JB64Header entries.  Each entry defines a column by which all records will be transformed and includes options to declare whether or not fields are allowed to be null.</param>
        /// <exception cref="JB64Exception">Thrown if the Map doesn't contain the exact same number of entries as the header.</exception>
        public void SetHeaderMap(List<JB64HeaderMap> Map)
		{
			if (this.Headers == null)  throw new JB64Exception("JSON-Base64 headers must be decoded before header mapping can be applied.");

            if (Map.Count < 1)  throw new JB64Exception("At least one JSON-Base64 header map must be specified.");
			if (Map.Count != this.Headers.Count)  throw new JB64Exception("JSON-Base64 header map does not have the same number of columns as the headers.");

            int x = 0;
            foreach (JB64HeaderMap Col in Map)
            {
                if (Col.Type.Length > 6 && Col.Type.Substring(0, 7) == "custom:" && this.Headers[x].Type != Col.Type)  throw new JB64Exception("JSON-Base64 map column " + (x + 1) + " has an invalid conversion type of '" + Col.Type + "' for header type '" + this.Headers[x].Type + "'.");

                x++;
            }

            this.Map = Map;
		}

        /// <summary>
        /// Accepts a JSON-Base64 string representing record data, decodes it, and maps it using the headers or the header map into a Dictionary.
        /// </summary>
        /// <param name="Line">A line of JSON-Base64 encoded input.</param>
        /// <returns>A Dictionary of key-value pairs.</returns>
        /// <exception cref="JB64Exception">Thrown if the record data is unable to be decoded.</exception>
        public Dictionary<string, JB64Value> DecodeRecordAssoc(string Line)
        {
            JB64Value[] Data = DecodeRecord(Line);

            int x = 0;
            Dictionary<string, JB64Value> Result = new Dictionary<string, JB64Value>();
            if (this.Map == null)
            {
                foreach (JB64Header Header in this.Headers)
                {
                    Result.Add(Header.Name, Data[x]);

                    x++;
                }
            }
            else
            {
                foreach (JB64HeaderMap Header in this.Map)
                {
                    Result.Add(Header.Name, Data[x]);

                    x++;
                }
            }

            return Result;
        }

        /// <summary>
        /// Accepts a JSON-Base64 string representing record data, decodes it, and maps it using the headers or the header map into an array of JB64Value entries.
        /// </summary>
        /// <param name="Line">A line of JSON-Base64 encoded input.</param>
        /// <returns>An array of JB64Value entries.</returns>
        /// <exception cref="JB64Exception">Thrown if the record data is unable to be decoded.</exception>
        public JB64Value[] DecodeRecord(string Line)
        {
            if (this.Headers == null)  throw new JB64Exception("JSON-Base64 headers must be decoded before record data can be decoded.");

            JArray Data = DecodeLine(Line);
            if (Data.Count != this.Headers.Count)  throw new JB64Exception("JSON-Base64 record data does not have the same number of columns as the headers.");

            // Convert the data and normalize the input.
            int x = 0;
            JB64Value[] Result = new JB64Value[Data.Count];
            foreach (JToken Col in Data)
            {
                if (Col.Type == JTokenType.Null)  Result[x] = new JB64Value();
                else if (Col.Type == JTokenType.Boolean)  Result[x] = new JB64Value((bool)Col);
                else if (Col.Type == JTokenType.Integer)  Result[x] = new JB64Value((int)Col);
                else if (Col.Type == JTokenType.Float)  Result[x] = new JB64Value((double)Col);
                else if (Col.Type == JTokenType.String)  Result[x] = new JB64Value((string)Col);
                else  throw new JB64Exception("Invalid JSON-Base64 column type found.");

                Result[x].ConvertTo(this.Headers[x].Type, "decode");

                x++;
            }

            // Map the result type.
            if (this.Map != null)
            {
                x = 0;
                foreach (JB64HeaderMap Header in this.Map)
                {
                    if (Result[x].Val == null && !Header.NullAllowed)  Result[x].ConvertToNotNull(Header.Type);

                    if (Result[x].Val != null)
                    {
                        if (Result[x].Type == "boolean" && Header.Type != "boolean")  Result[x].ConvertToInteger();

                        Result[x].ConvertTo(Header.Type);
                    }

                    x++;
                }
            }

            return Result;
        }

        private JArray DecodeLine(string Line)
        {
            Line = Line.Trim();

            if (Line.Length > 33 && Line[Line.Length - 33] == '.')
            {
                string TempMD5 = Line.Substring(Line.Length - 32);
                Line = Line.Substring(0, Line.Length - 33);
                if (TempMD5 != GetMD5Hash(Hash, Line))  throw new JB64Exception("JSON-Base64 encoded line contains an invalid MD5 hash.");
            }

            Line = Base64Decode(Line.Replace('-', '+').Replace('_', '/'));

            return JsonConvert.DeserializeObject<JArray>(Line);
        }

        private string Base64Decode(string Text)
        {
            while (Text.Length % 4 != 0)  Text += '=';

            var TextBytes = Convert.FromBase64String(Text);

            return Encoding.UTF8.GetString(TextBytes);
        }
    }
}
