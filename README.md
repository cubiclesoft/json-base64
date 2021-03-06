JSON-Base64
===========

The JSON-Base64 file format, aka JB64, was invented for performing clean data transfers of database records and spreadsheet values across a wide variety of Internet hosts, software programs, and programming languages. JB64 is encoded in a stream-ready format, follows a clearly defined specification, and the file format is released under the [Creative Commons Zero license](https://creativecommons.org/publicdomain/zero/1.0/legalcode-plain).  Reference implementations are released under a MIT or LGPL license, your choice.

This is the main GitHub repository for the source code.  If you are looking for implementations (i.e. stuff to download), see the Implementations page here:

http://jb64.org/implementations/

JSON-Base64 is a serious replacement for the CSV file format.  It enjoys wide support for the most popular software packages including Microsoft Excel, Google Sheets, and many database products including SQLite and MySQL/Maria DB.

[![Donate](https://cubiclesoft.com/res/donate-shield.png)](https://cubiclesoft.com/donate/) [![Discord](https://img.shields.io/discord/777282089980526602?label=chat&logo=discord)](https://cubiclesoft.com/product-support/github/)

Features
--------

* Supports multiple flat field types (booleans, integers, floating-point, strings, etc).
* Preserves Unicode character sequences.
* The first line contains a header record that defines each column name and its type.
* Each line contains exactly one record.
* Can be processed as a stream.
* Optional hash support for detecting accidental data corruption.
* Very easy to implement in most modern programming languages.
* Unencumbered by patents and is free to use.
* Designed for relatively painless integration into your project.
* Sits on GitHub for all of that pull request and issue tracker goodness to easily submit changes and ideas respectively.

More Information
----------------

For more information about JSON-Base64 itself, visit:

http://jb64.org/
