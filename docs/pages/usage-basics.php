<!DOCTYPE html>
<html>

<head>
    <title>Basic Conversion</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">Basic Conversion</h1>
    <hr>
    <p>This page covers how to convert to and from ABSave documents, as well as how to use maps for a more precise output and faster performance.</p>
    <p><b>Serialization</b> is converting data <i>to</i> ABSave and <b>deserialization</b> is converting data <i>from</i> ABSave.</p>

    <h2 id="autoConversion">Auto Conversion</h2>
    <hr>
    <p>When auto converting, ABSave will do everything "automatically". Simply give ABSave an object, and it will convert it for you.</p>
    <p><code>ABSaveDocumentConverter</code> is found under the namespace <code>ABSoftware.ABSave</code>, this can be used to easily convert ABSave documents.</p>
    <p>A lot of the conversion methods provided take in <code>ABSaveSettings</code>. There are two presets available built into ABSave listed below, and you should choose which one is best for your usage. You can find out more TODO SOMEWHERE</p>

    <ul>
        <li>
            <p><code>ABSaveSettings.PrioritizePerformance</code> - This means ABSave will focus on converting faster than making a small output.</p>
        </li>
        <li>
            <p><code>ABSaveSettings.PrioritizeSize</code> - This means ABSave will focus on making a smaller output than converting faster.</p>
        </li>
    </ul>
    <p></p>
    <p>You can then use <code>ABSaveDocumentConverter</code> to convert using one of these presets. Below are examples to convert a class called <code>MyClass</code>:</p>

    <pre><code class="language-csharp">
// Serialize into a byte array.
var bytes = ABSaveDocumentConverter.Serialize(obj, ABSaveSettings.PrioritizeSize);

// Serialize into a stream.
ABSaveDocumentConverter.Serialize(obj, stream, ABSaveSettings.PrioritizeSize);

// Deserialize from a byte array.
var result = ABSaveDocumentConverter.Deserialize&lt;MyClass&gt;(arr);

// Deserialize to a stream.
var result = ABSaveDocumentConverter.Deserialize&lt;MyClass&gt;(stream);
    </code></pre>

    <h2 id="mappedConversion">Mapped Conversion</h2>
    <hr>
    <p>Maps take more effort to setup. However, they are faster than auto serialization, can make smaller file sizes, and give more flexibility. A <b>map</b> tells ABSave exactly how something should be converted.</p>

    <div class="infoBox msgBox">
        <h4 class="noAnchor">INFO</h4>
        <p>You can selectively use auto for certain things in a map.
    </div>

    <h3 id="mappedConversion-typeConverters">Type Converters</h3>
    <hr>
    <p>In order to use maps, you need to first understand type converters.</p>
    <p>ABSave has a different type converter for all the built-in types it supports (and the user can make their own <a href="#" data-navigates="alongside" data-navigateTo="Custom Type Converters + Advanced Conversion">custom converters</a> too) - you'll find these under the namespace <code>ABSoftware.ABSave.Converters</code>.</p>
    <p>To use one of these, access the static <code>Instance</code> field on them. For example, <code>BooleanTypeConverter.Instance</code>, which would be used for <code>bool</code> types of data.</p>
    <p>You can see the full list of built-in type converters <a href="https://github.com/ABSoftwareOfficial/ABSoftware.ABSave/tree/master/ABSoftware.ABSave/Converters">here</a></p>

    <h3 id="mappedConversion-basicMap">Basic Map</h3>
    <hr>
    <p>This section will demonstrate how to convert the following object using a map. <code>MyOtherClass</code> is a different class that will be introduced later, for now we will just auto serialize that:</p>

    <pre><code class="language-csharp">
class MyClass 
{
    public bool MyBl;
    public int MyInt;
    public MyOtherClass MyOC;
}
    </code></pre>

    <p>To make a map for an object, we can create an instance of <code>ObjectMapItem</code>.</p>
    <p>In the constructor, we need to provide the following:</p>
    
    <ul>
        <li><p><b>Can be null</b> - This specifies whether the object can be null. Most of the map items need this specified. Choosing <code>true</code> can sometimes use up an extra byte in the ABSave output, so only enable it if it will ever be null.</p></li>
        <li><p><b>Constructor</b> - You must provide a way for ABSave to make an instance of your class, for deserialization. This is a <code>Func&lt;object&gt;</code>.</p></li>
        <li><p><b>Number of items</b> - You must specify how many fields you want to convert. Our object has 3 fields in it, and we want to convert all of them, so this will be "3" in this case.</p></li>
    </ul>

    <p>Here is an example for the <code>MyClass</code> type.</p>

    <pre><code class="language-csharp">
var map = new ObjectMapItem(false, () => new MyClass(), 3);
    </code></pre>

    <p>Then, to add items to the map, we can chain <code>AddItem</code> after it repeatedly. This method accepts the name of the member, and an <code>ABSaveMapItem</code>, which describes how the item should be converted.</p>
    <p>There are multiple types of <code>ABSaveMapItem</code>. For now we'll only use two of them:</p>
    <ul>
        <li><p><code>TypeConverterMapItem</code> - Converts an item using the given type converter. This takes whether the item can be null as the first parameter, which is then followed by which type converter to use.</p></li>
        <li><p><code>AutoMapItem</code> - Will automatically convert an item. We'll use this on the <code>MyOtherClass</code>.</p></li>
    </ul>
    
    <p>Here is the completed map, using <b>nameof</b> for better refactoribility:</p>

    <pre><code class="language-csharp">
var map = new ObjectMapItem(false, () => new MyClass(), 3)
    .AddItem(nameof(MyClass.MyInt), new TypeConverterMapItem(false, IntegerAndEnumTypeConverter.Instance))
    .AddItem(nameof(MyClass.MyBl), new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
    .AddItem(nameof(MyClass.MyOC), new AutoMapItem());
    </code></pre>

    <p>Finally, we can pass this to <code>ABSaveDocumentConverter</code>, giving it the a map to use as a guide.</p>

    <pre><code class="language-csharp">
var bytes = ABSoftwareDocumentConverter.Serialize(obj, ABSaveSettings.PrioritizeSize, map);
ABSoftwareDocumentConverter.Serialize(obj, map, stream);

var result = ABSoftwareDocumentConverter.Deserialize&lt;MyClass&gt;(obj, map, bytes);
var result = ABSoftwareDocumentConverter.Deserialize&lt;MyClass&gt;(obj, map, stream);
    </code></pre>

    <h3 id="mappedConversion-basicMap">Further Map</h3>
    <hr>
    <p>Instead of auto converting <code>MyOC</code>, we can declare another map for it, which specifies how to convert all the smaller parts of a <code>MyOtherClass</code>. To keep this simple, we'll imagine <code>MyOtherClass</code> only has one integer in it:</p>

    <pre><code class="language-csharp">
class MyOtherClass 
{
    public int AnInt;
}
    </code></pre>

    <p>Then, instead of creating a <code>AutoMapItem</code>, we create an instance of <code>ObjectMapItem</code>, and add the items like we did for our <code>MyClass</code>:</p>

    <pre><code class="language-csharp">
var map = new ObjectMapItem(false, () => new MyClass(), 3)
    .AddItem(nameof(MyClass.MyBl), new TypeConverterMapItem(false, BooleanTypeConverter.Instance))
    .AddItem(nameof(MyClass.MyInt), new TypeConverterMapItem(false, IntegerAndEnumTypeConverter.Instance))
    .AddItem(nameof(MyClass.MyOC), new ObjectMapItem(true, () => new MyOtherClass(), 1)
        .AddItem(nameof(MyClass.AnInt), new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance)));
    </code></pre>

    <p>This map still technically requires reflection in order to get and set the values of each item, and therefore isn't as high-performance as it can be, however it is still faster and more precise than without.</p>
    <p>It is possible to manually provide getters and setters too. Information about all the map items and how to do this is described in TODO WHERE</p>

    <pre><code class="language-csharp">
var map = new ObjectMapItem&lt;MyClass&gt;(3)
    .AddItem&lt;MyClass, int&gt;(nameof(MyClass.MyBl), new TypeConverterMapItem(BooleanTypeConverter.Instance))
    .AddItem&lt;MyClass, bool&gt;(nameof(MyClass.MyInt), new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance))
    .AddItem&lt;MyClass, MyOtherClass&gt;(nameof(MyClass.MyOC), new ObjectMapItem(1)
        .AddItem&lt;MyClass, int&gt;(nameof(MyClass.AnInt), new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance)));
    </code></pre>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">INFO</h4>
        <p>The <code>TItem</code> you give should match the exact type the field is, not the type of data that you've put into the field. This means if the field is an <code>object</code>, and an integer is put into it, the type given in the map should still be <code>object</code>.</p>
    </div>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>