<!DOCTYPE html>
<html>

<head>
    <title>Further Conversion</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">Library Structure</h1>
    <hr>
    <p>If you need to make your own converter, or need to use the more precise parts of ABSave, then it's important to understand how ABSave is structured and what ABSave uses to work.</p>

    <h2 id="readersAndWriters">Readers and Writers</h2>
    <hr>
    <p>To almost every method, ABSave passes a reader or a writer (depending on whether serializing or deserializating).</p>
    <p>This holds onto information about the current state of the document. And, contains methods to read/write primitive parts, such as bytes, strings or numbers to whatever source it's configured with.</p>
    <p>Making either of these requires two things:</b></p>

    <ul>
        <li><p><b>Stream</b> - This is the <code>Stream</code> that it reads/writes to.</p></li>
        <li><p><b>Settings</b> - These are the settings that it (and anything using it) will use.</p></li>
    </ul>

    <h3 id="writers">Writers</h2>
    <hr>
    <p>When serializing, ABSave passes around an <code>ABSaveWriter</code>. These parts of ABSave can then either access the current state of the document or use the helper methods on it to write data to the output.</p>
    <p>For example, if a converter needs to write a 32-bit integer into the output, it would do this: <code>writer.WriteInt32(i)</code></p>

    <p>Here is how to make a writer, providing the two things listed above:</p>

    <pre><code class="language-csharp">
var writer = new ABSaveWriter(stream, ABSaveSettings.PrioritizePerformance);
    </code></pre>

    <h3 id="readers">Readers</h2>
    <hr>
    <p>When deserializing, ABSave passes around an <code>ABSaveReader</code>. This also contains the current state and helper methods to <i>read</i> basic primitives out of the document.</p>
    <p>For example, reading a 32-bit integer from the source would be done like this: <code>int num = reader.ReadInt32()</code>.</p>

    <p>Here is how to make a reader, providing the two things listed above:</p>
    
    <pre><code class="language-csharp">
var reader = new ABSaveReader(stream, ABSaveSettings.PrioritizePerformance);
    </code></pre>

    <h2 id="structure">Structure</h2>
    <hr>
    <p>The ABSave library is organized into smaller parts, each of which are described below.</p>

    <h3 id="structure-document">ABSaveDocumentConverter</h3>

    <p>This was used in the first part. This is at the very top-level of the library, and provides easy helpers that make it so you don't have to worry about the readers and writers. A <b>document</b> is technically comprised of a <b>header</b> and item.</p>
    <p>The <b>header</b> stores the settings, this is why you don't have to provide settings for deserialization, which reduces the risk of having unmatching settings between serialization and deserialization.</p>
    <p><b>However, </b> if you do not use the document converter, you must ensure you use the same settings for serialization and deserialization, otherwise the data may be converted incorrectly.</p>
    <p>If you need better performance, you can usually avoid the document converter and manually call straight to <code>ABSaveObjectConverter</code> or <code>ABSaveItemConverter</code>, or even a type converter directly, just ensure the settings are the same.</p>

    <h3 id="structure-itemConverter">ABSaveItemConverter</h3>

    <p>An item represents a single value, whether it's null, and necessary type information in ABSave. If you want to know more information about how the binary format is internally structured, read <a data-navigates="exact" data-navigateTo="Binary%20Output>Document%20Structure.items">Document Structure Items</a>.</p>
    <p>The <code>ABSaveItemConverter</code> has two main methods on it:</p>

    <ul>
        <li><p><b>Serialize</b> - This writes a item, and will include the attribute if it's detects that it's necessary (it isn't necessary for value types).</p></li>
        <li><p><b>SerializeWithoutAttribute</b> - Writes an item without an attribute. Only ever use this if you know for certain that a value will never be null and will never change type (a sealed type or value type).</p></li>
    </ul>

    <h3 id="structure-itemConverter">Type Converter</h3>

    <p>These were briefly covered in the previous chapter.</p> 
    <p>Common types have their own type converter that write the items in a more efficient way. For example, <code>DateTime</code>s are simply an 8-byte integer, as opposed to being represented as a "whole object", where each field and their names are serialized.</p>
    <p>You can write your own Type Converter if you want to manually write a type a certain way.</p>

    <h3 id="structure-itemConverter">ABSaveObjectConverter</h3>

    <p>This is used for unrecognized values that don't have a type converter.</p>
    <p>This will manually go through each field in the object and convert each one, along with their names (ABSave does not support order-based objects due to reflection's inconsistency).</p>
    <p>Unless a map is specified, this will always run <code>ABSaveItemConverter.Serialize</code> on each item.</p>
    <p>If you want to know more information about how the binary format internally structures these "whole objects", read <a href="#" data-navigates="exact" data-navigateTo="Binary%20Output>Document%20Structure.wholeObjects">Whole Objects</a></p>.

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>