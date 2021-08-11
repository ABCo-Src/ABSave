<!DOCTYPE html>
<html>

<head>
    <title>More versioning</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">More versioning</h1>
    <hr>
    <p>This page goes a little more in-depth on how to correctly version your objects under <b>all scenarios</b>, handling all the different situations that could need versioning.</p>
    <p>As a reminder, these are the different changes you can make that could require a new 'version' to be introduced:</p>

    <ul>
        <li><p>Add a property (already covered in 'quick start')</p></li>
        <li><p>Remove a property</p></li>
        <li><p>Change a property's type</p></li>
        <li><p>Change something in one of ABSave's attributes (including the numbers in the <code>Save</code> attributes)</p></li>
    </ul>

    <h2 id="versioning-why">Why?</h3>
    <hr>
    <p>There's one question we haven't fully answered: Why <i>do we</i> need this versioning thing, while something like JSON doesn't? Well, here's why.</p>
    <p>In most human-readable serializers, such as JSON, when you give them a class to serialize, they not only write out the values of the properties, but they also write the names of <i>every single property</i> in the output as well.</p>

    <pre><code class="language-csharp">
[SaveMembers]
class MyClass
{
    [Save(0)]
    public int A { get; set; }

    [Save(1)]
    public int B { get; set; }

    [Save(2)]
    public int C { get; set; }
}
    </code></pre>

    <p>So in <b>JSON</b> the class above (from 'quick start') could be serialized like this:</p>
    <div class="img-box">
        <img src="../images/usage/JSONExample.png">
    </div>
    <p>However, this has some issues:</p>
    
    <ul>
        <li><p>If you ever refactor your object and change the name of a property, reading back that data now fails as it isn't able to find the property to match anymore because the name changed.</p></li>
        <li><p>A lot of time is spent not only writing all the names, but is especially spent having to find the correct property to match the name found in the file, therefore slowing down the process.</p></li>
        <li><p>This grows the output <b>a lot</b>. A lot of the output is now being used to store property names when that's not even the data we're trying to save</p></li>
    </ul>

    <p>So, to fix these issues, what ABSave and many other binary serializers do is they <b>don't</b> write out the names of the properties, they write <i>just</i> the values in the output, and then when it comes to reading those values back into the properties, they use the <i>order</i> those values appear to know which value goes to which property.</p>
    <p>So, in the case of the <code>MyClass</code> above, <code>A</code>'s value would come first, followed by property <code>B</code> etc.)</p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Note</h4>
        <p>On the <code>Save</code> attribute in the <code>MyClass</code> above, the numbers correspond to what order the property appears. So if you gave property <code>A</code> the number <code>1</code> and the property <code>B</code> the number <code>0</code>, then <code>B</code> would appear in the output first and then be followed by <code>A</code></p>
    </div>

    <p>This allows very to-the-point and compact output, as well as also making it much faster to both serialize and deserialize.</p>
    <p>However, there is one problem with this. If you ever <b>change the order</b> of the properties in the class, or <b>remove a property</b> or <b>add a property</b>, then ABSave will completely fail to read something that was serialized before the change. This is because ABSave tries to read the values from the document in the order described in the class, but because the class changed when they were serialized, the two no longer match anymore and as such ABSave tries to read it completely wrong.</p>
    <p>Most binary serializers don't provide any solution to this. But ABSave solves it with the versioning system.

    <h2 id="toVer">Using ToVer</h2>
    <hr>
    <p>In addition to the <code>FromVer</code> seen in the 'quick start' guide, most attributes also have a <code>ToVer</code> too.</p>
    <p>This lets you choose the (exclusive) <i>maximum version</i> a property applies to. For example, let's imagine we have the following property:</p>

    <pre><code class="language-csharp">
[Save(1, FromVer = 2, ToVer = 3)]
public int A { get; set; }
    </code></pre>

    <p>This property <code>A</code> is <b>only</b> available in version 2 of the document, and nothing below, and nothing above. The version '3' it references will now not have this property in it</p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Note</h4>
        <p>The <code>ToVer</code> will raise the highest version in the class. So if the class previously only had versions '0', '1' and '2', the property above will cause it to also have a '3' where the property is not present.</p>
    </div>
    
    <h2 id="remove-property">Removing a property</h2>
    <hr>
    <p><b>NOTE:</b> Make sure you've read the part about <a href="#toVer">ToVer</a> just above before reading this.</p>
    <p>We can remove a property and version it quite easily, simply add the <code>ToVer</code> described above to the property, and it will no longer be present in future versions, thus having removed it.</p>
    <p>Then, to customize what happens when we try to deserialize into the property, you can change the get and set to do what you'd like.</p>
    <p>If you want the property to be entirely ignored, you can make the getter and setter completely empty:</p>

    <pre><code class="language-csharp">
[Save(1, FromVer = 2, ToVer = 3)]
private int A { get => default; set { } }
    </code></pre>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Tip</h4>
        <p>You can make the property <code>private</code> so it doesn't distract anyone else.</p>
    </div>

    <p>Now this property takes up no room in memory so it's still just as efficient as it not being there, and you simply ignore any attempt to do anything with it.</p>
    <p>You can of course put whatever logic you'd like in the getter and setter however.</p>

    <h2 id="change-type">Retyping a property</h2>
    <hr>
    <p><b>NOTE:</b> Make sure you've read the part about <a href="#toVer">ToVer</a> at the top of this page before reading this.</p>
    <p>The best way to version a property changing a type is to make <i>two</i> properties. One property with the old type, and one property with the new type.</p>
    <p>Give each one the same number in their <code>Save</code> attribute but have the old type one apply to only the older version, and the new type one only the newer version.</p>
    <p>For example, say we had this property:</p>

    <pre><code class="language-csharp">
[Save(7)]
public int A { get; set; }
    </code></pre>

    <p>And if we wanted to change the type to <code>string</code>, we can introduce two properties like so:</p>

    <pre><code class="language-csharp">
[Save(7, ToVer = 1)]
private int _oldA 
{
     get => ...;
     set => ...; 
}

[Save(7, FromVer = 1)]
public string A { get; set; }
    </code></pre>

    <p>Now whenever we have an older document, ABSave will use the <code>_oldA</code> with the old type, and for newer ones it will use <code>A</code> with the new type.</p>
    <p>The finishing touch to this is you likely want to make the <code>_oldA</code> do all the conversion logic needed to put the correct value into <code>A</code> in its getter and setter, like so:</p>

    <pre><code class="language-csharp">
private int _oldA 
{
     get => int.Parse(A);
     set => A = value.ToString(); 
}
    </code></pre>

    <p>There we are, now whether old or new, it will correctly make its way into <code>A</code> as we want.</p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Tip #1</h4>
        <p>As seen above, you can make the old type property <code>private</code> so it doesn't distract anyone else.</p>
    </div>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Tip #2</h4>
        <p>You don't have to name it <code>_oldA</code> or name the new one <code>A</code>, ABSave pays absolutely <b>no attention</b> to the names at all, you can do whatever you like with them.</p>
    </div>

    <h2 id="reintroducing">Reintroducing a property</h2>
    <hr>
    <p><b>NOTE:</b> Make sure you've read the part about <a href="#toVer">ToVer</a> at the top of this page before reading this.</p>
    <p>If you remove a property and then decide in later versions you want to add it back again, then you can add <b>multiple</b> <code>Save</code> attributes to the property, to allow it to be available from multiple ranges of versions.</p>
    <p>For example, this property is only available in version '0', '1' and '3' and onwards, it's not in version 2 (remember, <code>ToVer</code> is exclusive so the first attribute is only versions '0' and '1'):</p>

    <pre><code class="language-csharp">
[Save(13, ToVer = 2)]
[Save(13, FromVer = 3)]
public int A { get; set; }
    </code></pre>

    <h2 id="multiple-attributes">Adding multiple attributes</h2>
    <hr>
    <p>Sometimes you can add <b>multiple</b> attributes to some things. For example, you can add multiple <code>SaveInheritance</code> attributes to a class, and multiple <code>Save</code> properties and such.</p>
    <p>And when you do this, attributes with higher <code>FromVer</code>s will always take priority. So if I have a property with the following two attributes:</p>

    <pre><code class="language-csharp">
[Save(13, FromVer = 1)]
[Save(19, FromVer = 3)]
public int A { get; set; }
    </code></pre>

    <p>If my version is 3 and above, then it will use the '19' attribute, because that takes priority over the lower '1 and above'. However, if my version is 1 or above and <i>below</i> 3, it will use the first attribute instead.</p>

    <h2 id="inheritance">Inheritance</h2>
    <h3 id="inheritance-info">Version inheritance info</h3>
    <hr>
    <p><b>NOTE:</b>This builds on top of what's covered in 'All Features', ensure you've read that first. Also make sure you read the <a href="#multiple-attributes">adding multiple attributes</a> section of this page before reading this.</p>
    <p>This section describes how to version any changes to the <code>SaveInheritance</code> info on an object, for details about how to handle changing the <code>SaveBaseMembers</code> attribute <a href="#inheritance-base">see here</a>.
    <p>Keep in mind that if you're using the <code>Index</code> mode of inheritance you <b>are</b> allowed to <i>add</i> to the <i>end</i> of the list without any impact on older versions or any reason to need versioning.</p>
    <p>Versioning the inheritance info is very easy, no matter whether you're changing the mode, or whether you're making a more complex change to the list, quite simply <i>all</i> you need to do is <i>add</i> a second attribute targetted at the newer version, like so:</p>

    <pre><code class="language-csharp">
[SaveMembers]
[SaveInheritance(SaveInheritanceMode.Index, typeof(SubOld))]
[SaveBaseMembers(SaveInheritanceMode.Index, typeof(SubNew), FromVer = 5)]
class Base
    </code></pre>

    <p>Now if we're on any version below <code>5</code>, it will go to the first attribute with the <code>SubOld</code> chosen. If we go to version 5 or above, it will go to the second attribute. That's it.</code>

    <p>If you're serializing the base members of a class and decide you want to change the base class you serialize the members of, then you'll need to introduce a new version.</p>

    <h3 id="inheritance-base">Version base members</h3>
    <hr>
    <p><b>NOTE:</b> Make sure you've not only read the section about how to serialize base members, but have also read the <a href="#multiple-attributes">adding multiple attributes</a> section just above before reading this.</p>
    <p>If you're serializing the base members of a class and decide you want to change the base class you serialize the members of, then you'll need to introduce a new version.</p>
    <p>You do <i>not</i> need a new version if you only change the base type of the class and it does not affect the attribute. Because the attribute remained the same, it doesn't matter.</p>
    <p>To version the change, simply add a <i>second</i> <code>SaveBaseMembers</code> attribute, like so:</p>

    <pre><code class="language-csharp">
[SaveMembers]
[SaveBaseMembers(typeof(A))]
[SaveBaseMembers(typeof(B), FromVer = 2)]
class MyClass : B
    </code></pre>

    <p>Now from version '2' and onwards, the base to have its items serialized will be class <code>B</code>'s members (and whatever B decides from there), but with version '0' and '1', it will be <code>A</code> only</p>

    <div class="msgBox warningBox">
        <h4 class="noAnchor">Warning</h4>
        <p>Version '0' and '1' will only work if <code>B</code> inherits from <code>A</code> somewhere down the chain. ABSave unfortunately cannot serialize members that <i>are not there</i>, and there is no way around this! You <b>must</b> keep the same base class if you're serializing that base class's members in any version.</p>
    </div>

    <p>Remember that the base and sub-class have seperate version numbers and as such will not affect each other</p>

    <h2 id="reordering">Reordering a property</h2>
    <hr>
    <p><b>NOTE:</b> Make sure you've read the section about <a href="#multiple-attributes">adding multiple attributes to things</a> before reading this.</p>
    <p>I'm not sure why you would ever want to change the position a property appears in within the output, but if you do for some reason, you can simply add two <code>Save</code> attributes to the property like so:<p>

    <pre><code class="language-csharp">
[Save(13, FromVer = 1)]
[Save(17, FromVer = 2)]
public int A { get; set; }
    </code></pre>

    <p>In version 1, it's now positioned wherever 13 falls in order and in version 2 and onwards positioned at wherever 17 falls in order.</p>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>