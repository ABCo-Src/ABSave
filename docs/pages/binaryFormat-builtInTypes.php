<!DOCTYPE html>
<html>

<head>
    <title>Binary Format</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">Built-In Types</h1>
    <hr>
    <p>
        This part contains information about how all the default, built-in, supported types are written. However, ABSave can be extended by the user to either serialize these types differently or serialize different types.<br><br>
        Make sure you've read the main part of the binary format documentation before reading this, which explains how topics such as endianness are generally handled.
    </p>

    <h2 id="number">Number</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/NumberBytes.png">
    </div>
    <p>Numbers are written in their full size, under the <b>target ABSave endianness</b>. For example, an <code>Int32</code> will take up four bytes, and be stored within those four bytes in the target endianness.<br><br></p>

    <h2 id="text">Text/String</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/StringBytes.png">
    </div>
    <p>
        Text starts with a <b>size</b>, which is a 4-byte number. This is then followed by the characters, which are also known as the data.<br><br>
        The size is the total number of characters the string has in it, not the number of bytes.<br><br>
        The characters are treated as 2-byte shorts (which are the bytes from the <code>char</code> data type). This means each character is two bytes big and <i>will match the target endianness</i>. There can never be a character that takes up less or more than 2 bytes.<br><br>
    </p>

    <h2 id="boolean">Boolean</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/BooleanBytes.png">
    </div>
    <p>
        Booleans take up one byte, which is <b>1</b> for true, and <b>0</b> for false. How other values are handled is <i>undefined behaviour</i>.<br><br>
    </p>

    <h2 id="ienumerables">IEnumerable</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/IEnumerableAndObjectBytes.png">
    </div>
    <p>
        <b>NOTE: Arrays are serialized differently from <code>IEnumerable</code>s as they can be multi-dimensional and have different lower-bounds.</b><br><br>
        First a size is written, this size is the number of items in the enumerable. Each item in the enumerable is then written sequentially.
    </p>

    <h2 id="array">Array</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/ArrayBytes.png">
    </div>
    <p>
        The first thing to be written for an array is a control byte. The least significant bit of this byte is 0 if any of the dimension's lower bounds are 0, 1 otherwise. The second least significant bit is 1 if this array is multi-dimensional (has more than one rank), 0 otherwise.<br><br>
        If the array is multi-dimensional, the rank is then written next.<br><br>
        The lower bounds for all dimensions are written next as 32-bit integers. <b>This is not written if all the lower bounds are 0.</b><br><br>
        The lengths for all dimensions are written next as 32-bit integers.<br><br>
        Finally, the items in the array are written. In a multi-dimensional array, the items are written in increasing indexes for each dimension, starting with the lower dimensions. Below is an example for an array <code>int[2, 2]</code>, the items are written in the order given:
    </p>

    <table class="docs-table">
		<tr>
			<td>0</td>
            <td>0</td>
		</tr>
        <tr>
			<td>0</td>
            <td>1</td>
		</tr>
        <tr>
			<td>1</td>
            <td>0</td>
		</tr>
        <tr>
			<td>1</td>
            <td>1</td>
		</tr>
	</table>

    <h2 id="version">Version</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/VersionBytes.png">
    </div>
    <p>
        We see a "default" version as <b>1.0.0.0</b>: A major of 1, and other information 0.<br><br>
        The first byte is a control byte, this specifies if the <code>Major</code>, <code>Minor</code>, <code>Build</code> or <code>Revision</code> (in this order) are different from the "defaults". This information is provided in the last 4 least significant bits of the control byte in the order specified, where "0" means they are the default and "1" means they are not.<br><br>
        After this byte, any of the four parts are then provided as 32-bit integers in this order: major, minor, build and revision. <b>If any of these are the default, they are skipped over and not written at all.</b>
    </p>

    <h2 id="assembly">Assembly</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/AssemblyBytes.png">
    </div>
    <p>
        <b>If types/assembly caching is enabled, a key is also written before the assembly, more information about caching types and assemblies can be found in "Document Structure".</b><br><br>
        The following four things are serialized with an <code>Assembly</code>: The assembly name, version, culture and PublicKeyToken.<br><br>
        The first byte is a control byte, this byte tells us whether the assembly is culture-neutral in the second least significant bit, and whether there's a <code>PublicKeyToken</code>, in the least significant bit. The culture-neutral bit is "1" if the assembly is culture-neutral, otherwise "0". The <code>PublicKeyToken</code> bit is "1" if the assembly has a <code>PublicKeyToken</code>, "0" otherwise.<br><br>
        Next is the assembly's <code>Name</code> and <code>Version</code>, these are always written.<br><br>
        This is followed by the assembly's <code>CultureName</code>. If the assembly is culture-neutral, this information is skipped.<br><br>
        This is followed by the assembly's <code>PublicKeyToken</code>, which is serialized as just the 8 bytes. This information is skipped if there is no <code>PublicKeyToken</code> (null).
    </p>

    <h2 id="types">Types</h2>
    <h3 id="types-generalStructure">General structure</h3>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/TypeGeneralBytes.png">
    </div>
    <p>
        <b>If types/assembly caching is enabled, a key is also written before the type, more information about caching types and assemblies can be found in "Document Structure".</b><br><br>
        In general, a type is made up of two parts: A <b>main part</b> and the <b>generic part</b>.<br><br>
        The main part stores everything but the generic information. The generic part stores all of the generic arguments of the type. If the type is <i>not</i> a generic type, then no generic arguments are written.
    </p>

    <h3 id="types-mainPart">Main part</h3>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/TypeMainPartBytes.png">
    </div>
    <p>
        The main part is made up of two parts: The type's <code>Assembly</code> and <code>FullName</code>, in the given order.<br><br>
        If the type is generic, the <code>FullName</code> will be used to create a <b>generic type definition</b>, which can then have its generic arguments filled in as the generic arguments are read next.
    </p>

    <h3 id="types-generics">Generic Arguments</h3>
    <hr>
    <p>
        All of the generic arguments are given in order one after the other. The deserializer knows how many generic arguments there are from the main part.<br><br>
        Here is how each generic argument is written:
    </p>
    <div class="img-box">
        <img src="../images/binaryFormat/TypeGenericBytes.png">
    </div>
    <p>
        Each generic argument has two parts. The first is a byte that specifies whether this argument is a generic parameter or not, if this is "1", then this argument is a generic parameter, and that's the only information that given.<br><br>
        If this argument is not a generic parameter, then this argument is followed by a type, and that type is whatever type of data this argument is.<br><br>
    </p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">NOTE</h4>
        <p>
            If a type is being serialized in the attributes, then the first byte is not written at all, and the generic argument is not treated as a generic parameter.<br><br>
            This is because when being serialized in the attributes, the type is guaranteed to be a <i>closed generic type</i>, so we don't need to store whether the generic arguments are generic or not, as they cannot be.<br><br>
        </p>
    </div>
    
    <h2 id="nullable">Nullable</h2>
    <hr>
    <p>
        Nullables first write an attribute, either "null" if the item is null or "matching type" if the item isn't. If it isn't null, this is then followed by the nullable's value data.
    </p>

    <h2 id="keyValuePair">KeyValuePair</h2>
    <hr>
    <p>
        <code>KeyValuePair</code>s are written as the <b>key</b> followed directly by the <b>value</b>.<br><br>
        These are commonly serialized for dictionaries when they are enumerated over.
    </p>

    <h2 id="enum">Enum</h2>
    <hr>
    <p>Enums are serialized as 4-byte integers</p>

    <h2 id="char">Character</h2>
    <hr>
    <p>Characters are serialized as 2-byte shorts.</p>

    <h2 id="dateTime">DateTime/TimeSpan</h2>
    <hr>
    <p>
        The <code>Ticks</code> are written.<br><br>
        It is written as an 8-byte long.<br><br>
        No extra information is written.
    </p>

    <h2 id="guids">Guids</h2>
    <hr>
    <p>A <code>GUID</code> is written as 16-bytes, with no size. These 16 bytes are the internal parts a - k (the result from <code>ToByteArray</code>).</p>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>