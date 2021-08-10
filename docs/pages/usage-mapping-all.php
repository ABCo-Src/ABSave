<!DOCTYPE html>
<html>

<head>
    <title>Mapping - All</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>

    <h1 id="title">All Features</h1>
    <hr>
    <p>This page gives you a list of all of the different features and the respective attributes available for them.</p>

    <h2 id="inheritance">Inheritance</h2>
    <hr>
    <p>From the start, one thing that was important for ABSave is its ability to represent inheritance. We've thought long and hard about how to do it in a nice and versionable way and this is how you do it!</p>
    <p>Now, when it comes to inheritance, there are <b>two</b> different types of attributes you can get. You'll usually want to combine these two together to achieve the support you're looking for.</p>

    <h3 id="inheritance-base">Save inheritance info</h3>
    <hr>
    <p>The first attribute is <code>SaveInheritance</code> and this one goes on a <b>base type</b> and allows you to have different sub-types of that class appear in the output. For example, you might have something like this:</p>

    <div class="img-box">
        <img src="../images/usage/InheritanceExample.png">
    </div>

    <p>And somewhere you have a <code>Base</code> object that could either be a <code>Sub1</code> or a <code>Sub2</code>. Without the attribute, ABSave will just serialize it like it's a <code>Base</code> and will <i>completely</i> ignore the inheritance, which is most likely not what you want.</p>
    <p>If you want ABSave to store in the output which one of the two sub-classes is present (so it retains that 'inheritance info'), then you can put the <code>SaveInheritance</code> attribute on the <code>Base</code> type. Simply provide a list of all the different sub-types of the class that could be present when there's a <code>Base</code> object somewhere, like so:</p>

    <pre><code class="language-csharp">
[SaveMembers]
[SaveInheritance(SaveInheritanceMode.Index, typeof(Sub1), typeof(Sub2))]
class Base { }
    </code></pre>

    <p>Now, ABSave is able to accurately say in the file which one of the two it is, whether it's a <code>Sub1</code> in the file or a <code>Sub2</code>!</p>
    <p>Make sure to also add <code>Base</code> to the list if you want ABSave to be able to store just a plain <code>Base</code> instance as well. If <code>Base</code> is abstract or there's never an instance of <code>Base</code> and it's always an instance of one of the sub-classes this won't be necessary</p>
    <p>You can <b>add</b> to <b>end</b> of the list as much as you'd like without breaking a previous version of the document, however, for any other change, take a look at the <a data-navigates="alongside" data-navigateTo="More Versioning.inheritance-info" href="#">inheritance section</a> of the 'More Versioning' page for more info of versioning.</p>
    <p>And you now <i>almost</i> have full support of your inheritance situation. However, you may also need the <a href="#inheritance-base">second attribute</a>, so make sure to look at that.</p>
    
    <div class="msgBox infoBox">
        <h4 class="noAnchor">Don't know the sub types?</h4>
        <p>If you're in a situation where you don't <i>know</i> all the different sub-types you might have (e.g. you're in a plugin situation), then don't panic! Take a look at the <a href="#inheritance-key">key inheritance mode</a> that's designed specifically so you can support that!</p>
    </div>

    <h3 id="inheritance-base">Serializing base members</h3>
    <hr>

    <p>There's also a second attribute you can get called <code>SaveBaseMembers</code>. This goes on the <b>sub-type</b> of a class and it tells ABSave that whenever it's serializing that type it should also serialize the base members.</p>
    <p>For example, consider this scenario:</p>
    
    <div class="img-box">
        <img src="../images/usage/BaseMembersExample.png">
    </div>
     
    <p>If we go to serialize a <code>Sub</code> object, ABSave will by default <i>only</i> serialize <code>B</code>, it will not pay attention to what the class inherits from, this is the default by design for a number of reasons. This applies even when we had a <code>Base</code> place which </p>
    <p>However, if you'd like ABSave to also serialize the base's members whenever you serialize <code>Sub</code>, you can add a <code>SaveBaseInheritance</code> attribute, like so:</p>
    <p>So, to enable that, all you need to do is add a <code>SaveBaseInheritance</code> attribute to <code>Sub</code>, telling it <i>which base type</i> you'd like to have serialized. Like so:</p>

    <pre><code class="language-csharp">
[SaveMembers]
[SaveBaseMembers(typeof(Base))]
class Sub : Base
{
    [Save(0)]
    public int B { get; set; }
}
    </code></pre>

    <p>This tells ABSave that whenever we serialize <code>Sub</code> we should include the members in the <code>Base</code> that we're inheriting from.</p>
    <p>When it comes to versioning, the base and sub have completely seperate version numbers and completely seperate numbers in the <code>Save</code> attribute, so don't worry about those, think of them as two different classes.</p>
    <p>Note that the type you give <code>SaveBaseMembers</code> doesn't have to be the very first thing the class inherits from. You could do <i>this</i> if you want:</p>

    <pre><code class="language-csharp">
[SaveMembers]
class A
{
    [Save(0)]
    public int A { get; set; }
}

[SaveMembers]
class B : A
{
    [Save(0)]
    public int B { get; set; }
}

[SaveMembers]
[SaveBaseMembers(typeof(A))]
class C : B
{
    [Save(0)]
    public int C { get; set; }
}
    </code></pre>

    <p>Now when serializing <code>C</code> ABSave will only include members from <code>A</code> and <code>C</code>.</p>
    <p>If you wanted to have <code>A</code>, <code>B</code> <i>and</i> <code>C</code> all have their members included when serializing <code>C</code>, you can do this:

    <pre><code class="language-csharp">
[SaveMembers]
class A
{
    [Save(0)]
    public int A { get; set; }
}

[SaveMembers]
[SaveBaseMembers(typeof(A))]
class B : A
{
    [Save(0)]
    public int B { get; set; }
}

[SaveMembers]
[SaveBaseMembers(typeof(B))]
class C : B
{
    [Save(0)]
    public int C { get; set; }
}
    </code></pre>

    <p>Now when you serialize <code>C</code> ABSave will include <code>B</code>, and because <code>B</code> then says it requires <code>A</code> all three will have their members included.</p>
    <p>For info about versioning, take a look at the <a data-navigates="alongside" data-navigateTo="More Versioning.inheritance-base" href="#">inheritance section</a> of the 'more versioning' page.</p>

    <h2 id="member-ordering">Member ordering</h2>
    <hr>
    <p>This part covers what exactly the number in the <code>Save</code> attributes mean.</p>
    <p>The number in the <code>Save</code> attribute essentially defines the 'order' in which the properties' values are put in the file whenever that class is serialized</p>
    <p>When ABSave is serialized, it doesn't store the names of every property like a format such as JSON, it instead stores <i>only the values</i> of those properties, meaning ABSave uses the order those properties appear in to know which value goes to which property in a class.</p>
    <p>The numbers in the <code>Save</code> attribute determine this order. ABSave will sort all the properties in ascending order, the one with the lowest number coming first. You should ensure the order of these numbers always remains the same for versionability, but if you for whatever reason ever want to change the order, see <a data-navigates="alongside" data-navigateTo="More Versioning.reordering" href="#">here</a> the best way to version that.</p>

    <h3 id="key-inheritance">Key inheritance mode</h3>
    <hr>
    <p>If you're in a plugin-type scenario where you <i>don't know</i> what sub-types of a class you can have, then don't worry. You can use <b>key inheritance</b> mode.</p>

    <div class="img-box">
        <img src="../images/usage/InheritanceKeyExample.png">
    </div>

    <p>To use this, you first need to add the attribute <code>SaveInheritance</code> to the base just like before, but this with using the "key" mode.</p>
    <p>Once you've got that, you can then <i>add</i> a <code>SaveInheritanceKey</code> attribute with a unique key only that sub-class will have:</p>

    <pre><code class="language-csharp">
[SaveMembers]
[SaveInheritance(SaveInheritanceMode.Key)]
class Base { }

[SaveMembers]
[SaveInheritanceKey("Sub")]
class Sub : Base { }
    </code></pre>
    <p>What will happen now is ABSave will write <i>that key</i> the first time a given sub-class occurs in the file, and when deserializing it will find the type with that key. So just make sure each sub-class has a key.</p>
    <p>If you'd know what <i>some</i> of the sub-types will be, but know there may be others, like shown below, then you can combine the "index" mode for above and "key" mode.</p>

    <div class="img-box">
        <img src="../images/usage/InheritanceKeySomeKnownExample.png">
    </div>

    <p>When you do this, anything that's in the list will be written as a number in file (therefore saving some space as there's no key being written), and anything that's not will then attempt to be found by key.</p>
    <p>To combine, the modes simply provide the attribute with <code>SaveInheritanceMode.Index | SaveInheritanceMode.Key</code></p>

    <h2 id="fields">Field Serialization</h2>
    <hr>
    <p>We don't recommend serializing fields, they're harder for ABSave to deal with performance-wise, they have to be turned into properties if you going to remove or change the type of one of them and want that to be versioned, just in general it's not a great idea, and it's ideal to just use properties.</p>
    <p>However, if you want it, we do of course provide it. Simply add <code>SaveMembersMode.Fields</code> to the <code>SaveMembers</code> attribute on your class, like so:</p>

    <pre><code class="language-csharp">
[SaveMembers(SaveMembersMode.Fields)]
class Cl { }
    </code></pre>

    <p>Now you can mark up the fields with <code>Save</code> and all of that just like you would a property.</p>
    <p>You can also have ABSave serialize both fields <i>and</i> properties in a class, by using <code>SaveMembersMode.Fields | SaveMembersMode.Properties</code>.</p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Info</h4>
        <p>When you do this, fields and properties will be treated equally by the <code>Save</code> attributes, so you need to make sure no field or property have the same number in their <code>Save</code> attribute</p>
    </div>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>