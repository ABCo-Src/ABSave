<!DOCTYPE html>
<html>

<head>
    <title>Usage High Performance</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>
    
    <h1 id="title">2. Further Conversion</h1>

    <h2 id="mappedConversion-advancedMap">High-Performance Map</h2>
    <hr>
    <p>In the last part we made a map that specifies all the members of <code>MyClass</code>, which looks like this:</p>

    <pre><code class="language-csharp">
class MyClass 
{
    public bool MyBl;
    public int MyInt;
    public MyOtherClass MyOC;
}
...
var map = new ObjectMapItem(3)
    .AddItem(nameof(MyClass.MyBl), new TypeConverterMapItem(BooleanTypeConverter.Instance))
    .AddItem(nameof(MyClass.MyInt), new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance))
    .AddItem(nameof(MyClass.MyOC), new ObjectMapItem(1)
        .AddItem(nameof(MyClass.AnInt), new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance)));
    </code></pre>

    <p>However, reflection is still involved in order to get/set the values on the items. For maximum performance, you can manually provide a getter and setter for each map item, you should generally only do this if <b>absolutely necessary</b> and for large objects can become very difficult to maintain.</p>
    <p>We can provide these using a generic version of <code>AddItem</code>, with different parameters, this is <code>AddItem&lt;TObject, TItem&gt;</code>. <code>TObject</code> is the type of the object we're making this map for, in this case <code>MyClass</code>. And the <code>TItem</code> is what type of data this field contains.</p>
    <p>This is a map where all the items have manual getters and setters:</p>

    <pre><code class="language-csharp">
var map = new ObjectMapItem&lt;MyClass&gt;(3)
    .AddItem&lt;MyClass, bool&gt;(nameof(MyClass.MyBl), o => o.MyBl, (o, v) => o.MyBl = v, new TypeConverterMapItem(BooleanTypeConverter.Instance))
    .AddItem&lt;MyClass, int&gt;(nameof(MyClass.MyInt), o => o.MyInt, (o, v) => o.MyInt = v, new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance))
    .AddItem&lt;MyClass, MyOtherClass&gt;(nameof(MyClass.MyOC), o => o.MyOC, (o, v) => o.MyOC = v, new ObjectMapItem(1)
        .AddItem&lt;MyClass, int&gt;(nameof(MyClass.AnInt), o => o.AnInt, (o, v) => o.AnInt = v, new TypeConverterMapItem(IntegerAndEnumTypeConverter.Instance)));
    </code></pre>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>