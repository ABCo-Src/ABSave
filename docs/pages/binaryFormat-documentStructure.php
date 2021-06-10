<!DOCTYPE html>
<html>

<head>
    <title>Binary Format</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="documentStructure">Document Structure</h1>
    <h2 id="header">Header</h2>
    <hr>
    <p>An ABSave document starts with a header, to help determine how the ABSave document is configured, this is one byte big.</p>
    <p>The least significant bit stores which endianness the target ABSave is. This is what endianness all number types will be stored as. "1" represents little endian and "0" represents big endian.</p>
    <p>The next least significant bit stores whether to <a href="#typeCaching">cache types and assemblies</a>. Disabling this will usually make the document longer unless you know that all types and assemblies will never appear more than once, in which case it will shorten the document.</p>

    <h2 id="items">Items</h2>
    <hr>
    <p>An item represents a piece of data in ABSave. It could be an item in an array or a member in an object. It is made up of two parts: The attribute, and the actual data for that piece of data.</p>
    <p>When we consider items, they have a <b>specified type</b> and an <b>actual type</b>. The <b>actual type</b> is the exact type of the value we're serializing. The <b>specified type</b> is what type the container describes it as.</p>
    <p>For example, if we had the array <code>object[]</code>, with 1 integer in it, the <b>actual type</b> would be <code>int</code> while the <b>specified type</b> would be <code>object</code>. The same applies to members of a class.</p>
    <p>It's important to remember that at deserialization we only have the specified type.</p>

    <h3 id="items-attributes">Attributes</h3>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/ItemBytes.png">
    </div>
    <p>The attributes store the <b>type information</b> (if strongly-typed is enabled) and whether the data is null. The first byte specifies <i>what</i> attribute there is.</p>
    <p>If the data is null, the first byte will be "1", and <b>that's all that will be written for the entire item</b>.</p>
    <p>If the <b>actual type</b> and <b>specified type</b> match, a "2" is written, then the data will be written.</p>
    <p>Otherwise, if the <b>actual type</b> and <b>specified type</b> don't match, a "3" is written, then type information is written, and then the item's data is written.</p>

    <div class="msgBox warningBox">
        <h4 class="noAnchor">IMPORTANT</h4>
        <p>
            If the <b>specified type</b> is a value type, no attributes are written as it is guaranteed that it cannot be null or have a different actual type.
        </p>
    </div>

    <h3 id="items-data">Data</h3>
    <hr>
    <p>The data is the data this item is made up of. It will either be serialized using a converter (you can see the built-in supported types in <a href="#" data-navigates="alongside" data-navigateTo="Built-in Types">built-in types</a>) or if no suitable converter can be found, serialized as a <a href="#wholeObjects">whole object</a>.

    <h2 id="wholeObjects">Whole Objects</h2>
    <hr>
    <div class="img-box">
        <img src="../images/binaryFormat/IEnumerableAndObjectBytes.png">
    </div>
    <p>If there is no converter for a type, all the members must be written.</p> 
    <p>First, a size is written specifying how many members will be written.</p>
    <p>Then, for each member, the name is first written (as a string), then the item's data can be serialized, following how <a href="#items">items</a> are serialized.</p>
    <p>Below is an illustration of <b>each member</b>.</p>
    <div class="img-box">
        <img src="../images/binaryFormat/ObjectMemberBytes.png">
    </div>

    <h2 id="typeCaching">Types/Assembly Caching</h2>
    <hr>
    <p><b>A "cachable" refers to a type or assembly.</b></p>
    <p>If type/assembly caching is enabled, then a key is written before cachables. This key is a numerical number. The first time a certain cachable is encountered in a document, ABSave writes a key before the cachable.</p>
    <p>Now, if that exact cachable occurs again, then only that key is needed to reference that cachable, and the type information doesn't have to get written.</p>
    <p>Keys should always be <i>added</i> in increasing order: You can't have the key "1" and then the key "3" <i>on new cachables</i> after each other in a document. The keys for types and the keys for assemblies are seperate from each other.</p>
    <p><b>The keys are written differently from other numbers.</b> They are <b>always</b> written as little endian, regardless of the target endianness, and the byte size of the key will increase as the document progresses.</p>
    <p>When less than 256 cachables have been encountered, it will only be 1 byte. However, above 255 cachables, all keys will be written using 2 bytes, regardless of whether it was a key from before. This pattern continues up to 4 bytes (< 16,777,216 = 3, < 2,147,483,647 = 4).</p>
    <p>If more than 2,147,483,647 cachables are reached, and any new types/assemblies are encountered, <code>FFFFFFFF</code> is written for them. This number will not be treated as a key, and marks that there is no key due to limits. This number can only be written if we've reached the limit.</p>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>