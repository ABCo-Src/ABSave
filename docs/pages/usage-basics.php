<!DOCTYPE html>
<html>

<head>
    <title>Quick Start</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">Quick Start</h1>
    <h2 id="quick-start">Quick Start</h2>
    <hr>

    <p>ABSave is packed internally with so many features that allow it to achieve a very compact output and very fast serialization times, and all of that is available for just a few lines of code!</p>
    <p>This section is designed to get you going within <i>minutes</i>, and the next part of this page, "versioning", will take a look at a very fundamental issue that almost every binary serializer suffers from, as well as the feature you can use in ABSave that's able to fix it!</p>
    
    <h3 id="class-marking">Preparing a class</h3>
    <hr>
    <p>As with every binary serializer in existence, the very first thing you need to do to use ABSave is add some very simple attributes to the classes you want to have serialized, every binary serializer needs these, it is physically impossible for them to function without.</p>
    <p>To tell ABSave you want it to serialize the smaller properties of a class, simply add the <code>[SaveMembers]</code> attribute to the class and any sub-classes you want to also have serialized member-by-member, like so:</p>

    <pre><code class="language-csharp">
[SaveMembers]
class MyClass { }
    </code></pre>

    <p>Just like that, ABSave now knows it needs to serialize the smaller properties in this class. Now all we need to do is add a <code>[Save]</code> attribute to each property, with an increasing number.</p>

    <pre><code class="language-csharp">
[SaveMembers]
class MyClass
{
    [Save(0)]
    public int A { get; set; }

    [Save(1)]
    public int B { get; set; }

    [Save(2)]
    public string C { get; set; }
}
    </code></pre>

    <p>These numbers let ABSave know what order to write the properties' values in within the output - just make sure each property has a unique number and you'll be fine.</p>
    <p>And that's it! This class is now ready for (de)serialization!</p>
    <p>There is <i>so much</i> more you can do with these attributes, such as inheritance or serializing fields, and after you've read this page, you can go to the <a href="#" data-navigates="alongside" data-navigateTo="Mapping">mapping page</a> and take a look at everything these attributes have to offer!</p>

    <!--<ul>
        <li><p>Inheritance - If you want <b>inheritance</b> support on this object, take a look here for info about attributes needed to support inheritance.</p></li>
        <li><p>Serialize fields - If you want to serialize fields instead of properties (although we don't recommend it) take a look here for info on how to do it.</p></li>
        <li><p>Settings Mapping - If you can't add attributes to the class (i.e. it's in another assembly), then don't worry, take a look here to find out what you can do.</p></li>
    </ul>-->

    <h3 id="serialize-and-deserialize">Serializing & Deserializing</h3>
    <hr>
    <p>Now let's serialize it! In order to serialize a class, we need to get something called a <code>ABSaveMap</code>. And we can do that like so:</p>

    <pre><code class="language-csharp">
var map = ABSaveMap.Get&lt;MyClass&gt;(ABSaveSettings.ForSpeed);
    </code></pre>

    <p>You can replace that <code>ABSaveSettings.ForSpeed</code> with <code>ABSaveSettings.ForSize</code> if you'd rather ABSave went a little slower to give you a smaller output.</p>

    <div class="msgBox warningBox">
        <h4 class="noAnchor">Create this once</h4>
        <p>You should only create the map for a class <b>once</b> and hold it in a static member of some kind. You can then continually re-use the same map each time you serialize.</p>
    </div>

    <p>Now that we have our map, we can simply <b>serialize</b> by passing our instance and our map into <code>ABSaveConvert.Serialize</code>, whether you want to output into a byte array or a stream is up to you:</p>

    <pre><code class="language-csharp">
// Outputting into a byte array:
byte[] arr = ABSaveConvert.Serialize(obj, map);

// Outputting into a stream:
ABSaveConvert.Serialize(obj, map, stream);
    </code></pre>

    <p>And <b>deserializing</b> is just as easy:</p>

    <pre><code class="language-csharp">
// Deserializing from a byte array:
MyClass obj = ABSaveConvert.Deserialize&lt;MyClass&gt;(byteArray, map);

// Deserializing from a stream:
MyClass obj = ABSaveConvert.Deserialize&lt;MyClass&gt;(stream, map);
    </code></pre>

    <p>And <i>that</i> is all there is to using ABSave at first, enjoy the very high performance and very compact output! However, <b>make sure to read the next section</b> for some important information that you need, especially if you ever change the classes used in ABSave.</p>

    <h2 id="versioning">Versioning</h2>
    <hr>
    <p>Just like that, you know the basics of how to use ABSave. However, just before you can go using it in larger applications there's just one thing you should be aware of.</p>
    <p>Almost <b>every</b> binary serializer in existence, including ABSave, has one major issue: If you ever change a class, it will fail to read anything that was serialized before that change. 
    <p>You can find out more about why this happens in the FAQ section of these docs, but this is how almost all binary serializers go.</p>
    <p>If you do any of these things, anything that had been serialized prior to the change will fail to read:</p>

    <ul>
        <li><p>Add a property</p></li>
        <li><p>Remove a property</p></li>
        <li><p>Change a property's type</p></li>
        <li><p>Change something in one of ABSave's attributes (including the numbers in the <code>Save</code> attributes)</p></li>
        <li><p>Reorder the properties</p></li>
    </ul>

    <p>These are the only things that will break a previous ABSave document from being read correctly, you're free to do absolutely anything else (so you <i>can</i> refactor the property names or move them up and down in the class as much as you'd like, so long as the numbers on the <code>Save</code> stays the same they'll be kept in the right place).</p>

    <p>But still, this is quite an issue, and unfortunately <b>a lot</b> of binary serializers provide no solution to this problem! Meaning you must somehow <b>never</b> change the class for the output to continue to be readable, which just isn't practical for larger applications.</p>
    <p>However, ABSave has a very easy-to-use and effortless system to <b>solve this problem</b>.</p>

    <h3 id="versioning-absave-solution">ABSave's solution</h3>
    <hr>

    <p>So, ABSave's solution is quite simple: Use just a <i>few bits</i> in the output to give each class a <b>version number</b>. And each different 'version' of a class has a different set of members or attributes applied.</p>
    <p>So whenever you make a change to a class or make a set of changes to a class as above, what you do is you essentially introduce a new version, a new variant of the class, with those changes.</p>
    <p>Of course, this sounds really <b>hard</b> and <b>painful</b> to maintain - but, the great thing is it's completely seamless and works very well with the attributes.</p>

    <h3 id="versioning-example">Example</h3>
    <hr>

    <p>Let's say we have the following class:</p>

    <pre><code class="language-csharp">
[SaveMembers]
class MyClass
{
    [Save(0)]
    public int A { get; set; }

    [Save(1)]
    public int B { get; set; }
}
    </code></pre>

    <p>And we need to <i>add</i> a new property to it. But, we've been serializing this class for some time and we don't want to break any existing serialized output.</p>
    <p>All we need to do is when we add the new property, we give it the <code>Save</code> attribute as before, <b>but</b> with a <code>FromVer = 1</code> on it, like so:</p>

    <pre><code class="language-csharp">
[SaveMembers]
class MyClass
{
    [Save(0)]
    public int A { get; set; }

    [Save(1)]
    public int B { get; set; }

    [Save(2, FromVer = 1)]
    public int C { get; set; }
}
    </code></pre>

    <p>What this is doing is it's telling ABSave is that our new property <code>C</code> is <b>only</b> present starting <i>from</i> version '1' and upwards on this class. So we essentially have <b>two</b> versions now: Version '0' has <code>A</code> and <code>B</code> while version '1' has <code>A</code>, <code>B</code> and <code>C</code>.</p>
    <p>If we ever added another property or set of properties, we'd give them <code>FromVer = 2</code>, telling ABSave that they only occur from version '2' and upwards, and therefore giving the class three different versions.</p>
    <p>And that's all it takes, problem solved! ABSave just makes it all seamlessly happen! You can find out more about how to correctly 'version' all the changes listed above in the <a href="#" data-navigates="alongside" data-navigateTo="Mapping">mapping</a> section.</p>

    <h2 id="going-from-here">Going from here</h2>
    <hr>
    <p>That's it! You can now go ahead and use ABSave as much as you'd like. If you want to learn even more about using ABSave now, here are some of the other pages you can go to:</p>
    <div class="navBoxContainer">
        <div data-navigates="alongside" data-navigateTo="Mapping" class="navBox navBoxLight navBox-half">
            <h1 class="noAnchor">Mapping</h1>
            <p>Find out about all the different attributes and their abilities!</p>
        </div>
        <div data-navigates="alongside" data-navigateTo="Settings" class="navBox navBoxLight navBox-half">
            <h1 class="noAnchor">Settings</h1>
            <p>Play about with the individual options within <code>ABSaveSettings</code> to tune ABSave to exactly your scenario!</p>
        </div>
    </div>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>