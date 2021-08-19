<!DOCTYPE html>
<html>

<head>
    <title>Custom Converters</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>
    
    <h1 id="title">Custom Converters</h1>
    <hr>
    <p>ABSave was built from the ground up to be as extendable and customizable as possible, and that customizability is allowed through the <b>custom converter</b> system it provides.</p>
    <p>When you're using ABSave in larger scenarios, there are sometimes objects represented in such a way that simply annotating properties with <code>Save</code> attributes isn't enough to accurately portray how you wish them to be serialized.</p>
    <p>If you want a type to be serialized in a very specific way everytime it's encountered, you can define your own converter for that type, with its own completely custom serialization and deserialization code.</p>
    
    <div class="msgBox infoBox">
        <h4 class="noAnchor">Fun fact</h4>
        <p>Inside ABSave, every single type that gets given special treatment (like integers, arrays, strings etc.), is given that special treatment <i>via converters</i>. ABSave internally defines a bunch of converters for all the types it supports, to serialize them the best way possible. So when you define your own custom converter, you have <i>all</i> the same capabilities available as the built-in ABSave types have, as you're doing the same as what's inside ABSave.</p>
    </div>

    <p>This page describes how to create your own custom converter, and all of the features you have available to make the converter as effective as possible.</p>

    <h2 id="creating-class">Creating the class</h2>
    <hr>
    <p>All a converter is to ABSave, is just a class that inherits <code>ABCo.ABSave.Serialization.Converters.Converter</code>. Once you've created it, you can then add it to your <code>ABSaveSettings</code> object to use it in a serialization.</p>
    <p>So, simply create a class alongside these lines, and you'll have a "converter", the name you give it is completely irrelevant:</p>

    <pre><code class="language-csharp">
public class ABColorConverter : Converter { ... }
    </code></pre>

    <h2 id="type-selection">Type selection</h2>
    <hr>
    <p>Now the class has been created, the first step in creating our converter is to let ABSave know <i>what types</i> it's even supposed to serialize with our converter! We call this "select"ing the type for the converter.</p>
    <p>The flowchat below describes what happens <i>per each converter</i> when ABSave first encounters a type, and how it decides per each converter if they should be used for a certain type or not. The smaller parts of this flowchart are described in more detail later.</p>

    <div class="img-box">
        <img src="../images/usage/TypeSelectionExample.png">
    </div>

    <p>NOTE: It is technically slightly more complex than this internally, doing all sorts of things with hash tables and such to achieve the best performance, but none of that matters, this is what you visually see outside of ABSave.</p>

    <h3 id="type-selection-byAttr">Selecting by attribute</h3>
    <hr>
    <p>The most common way to <i>select</i> what type our converter will serialize is using the very first thing in the flowchart, the <code>Select</code> attributes.</p>
    <p>Simply add a <code>Select</code> attribute, passing it what <i>type</i> of item you'd like to have your converter serialize. For example, if you had a type called <code>ABColor</code> and you wanted to make a converter for that, you'd add this:</p>

    <pre><code class="language-csharp">
[Select(typeof(ABColor))]
public class ABColorConverter : Converter { ... }
    </code></pre>

    <p>Now if ABSave sees a <code>ABColor</code>, it will use your converter for it.</p>
    <p>If your converter serializes <i>multiple</i> different types, you can add <b>multiple</b> <code>Select</code> attributes to your converter, like so.</p>

    <pre><code class="language-csharp">
[Select(typeof(ABColor))]
[Select(typeof(ABGradientColor))]
public class ABColorConverter : Converter { ... }
    </code></pre>

    <p>If your converter serializes a <i>generic</i> type, the <code>Select</code> attributes can deal with that too:</p>

    <pre><code class="language-csharp">
// If you want to always select a type completely regardless of what generic argument it has, provide an open type, like so:
[Select(typeof(ABColor&lt;&gt;))]

// Or, if you want to select only specific generic arguments, simply providing the full types like before will work fine.
[Select(typeof(ABColor&lt;Solid&gt;))]
[Select(typeof(ABColor&lt;Gradient&gt;))]
    </code></pre>

    <p>The <code>Select</code> attributes do not accept "partially open" types, where you ignore some generic arguments and not others. See the next bit for an alternative if you need that.</p>

    <h3 id="type-selection-custom">Selecting by CheckType</h3>
    <hr>
    <p>As great as the <code>Select</code> attributes are... They can't cover everything you could possibly want.</p>
    <p>If you have very specific selection requirements, you can override a method called <code>CheckType</code> with your own completely custom selection code.</p>
    <p>First, you need to enable the method by adding the attribute <code>SelectOtherWithCheckType</code>. This tells ABSave to select <i>other types</i> ("other" being anything not caught by the <code>Select</code> attributes) with <code>CheckType</code>.</p>
    <p>Next, simply override the method, and if none of your <code>Select</code> attributes match the type (if you have any at all), ABSave will call <code>CheckType</code>. If it returns <code>true</code>, the converter is used for the type.</p>

    <pre><code class="language-csharp">
[SelectOtherWithCheckType]
public class ABColorConverter : Converter
{
    public override bool CheckType(CheckTypeInfo info) { ... }
}
    </code></pre>

    <p>Within the <code>CheckTypeInfo</code> you get passed in, you'll find the <code>Type</code> we're checking this to see if this converter fits, as well as the <code>ABSaveSettings</code>, in case that influences your decision.</p>
    <p>So, as an example, if you had a converter that serializes both <code>ABColor</code> <i>and</i> everything that <i>inherits from</i> <code>ABColor</code> too, you could do this:</p>

    <pre><code class="language-csharp">
[Select(typeof(ABColor))]
[SelectOtherWithCheckType]
public class ABColorConverter : Converter
{
    public override bool CheckType(CheckTypeInfo info) => info.Type.IsSubclassOf(typeof(ABColor));
}
    </code></pre>

    <p>If the type is <code>ABColor</code>, it immediately gets picked up by the <code>Select</code>. If not, it will call <code>CheckType</code>, where we'll check if it inherits an <code>ABColor</code> or not.</p>

    <h2 id="initialization">Serialization</h2>
    <hr>
    <p>If you look at the flowchart above, you'll notice that right before it finishes, it calls a method titled <code>Initialize</code> on our converter. This is a very useful method you can override, and we will going to get to that method in a moment. But first, how about we finally get to doing some actual serializing!</p>
    <p>Simply override the two abstract methods <code>Serialize</code> and <code>Deserialize</code>, and they will be called when the given type needs to be serialized (written) or deserialized (read) respectively. Just like so:</p>

    <pre><code class="language-csharp">
[Select(typeof(ABColor))]
public class ABColorConverter : Converter
{
    public override void Serialize(in SerializeInfo info)
    {
        // Get the serializer, this is what we write into. We'll find out what "info.Header" is later.
        // Just know that if you're not going to write anything to the output, you should not call this "Finish" method here.
        ABSaveSerializer serializer = info.Header.Finish();

        // Serialization logic
    }

    public override object Deserialize(in DeserializeInfo info)
    {
        // Get the deserializer, this is what we read from.
        ABSaveDeserializer deserializer = info.Header.Finish();

        // Deserialization logic
    }
}
    </code></pre>

    <p>You'll notice that in both methods we retrieve the <code>ABSaveSerializer</code> and <code>ABSaveDeserializer</code>, these are the <b>central</b> objects in charge of running the entire serialization process. And we read/write to these objects.</p>
    <p>So, let's make an actual converter for <code>ABColor</code> with this. Let's imagine that all we want to store about an <code>ABColor</code> is a single <code>int</code> called <code>ColorValue</code>, and that that's all we want to store.</p>
    <p>To do that, we'll just call <code>WriteInt32</code> when serializing and <code>ReadInt32</code> when deserializing, like so, and that's all it takes:</p>

    <pre><code class="language-csharp">
[Select(typeof(ABColor))]
public class ABColorConverter : Converter
{
    public override void Serialize(in SerializeInfo info)
    {
        // info.Object - The instance you're serializing
        var obj = (ABColor)info.Object;

        // info.Header - The "header", just call "Finish" on this for now to get the serializer object to write into.
        var serializer = info.Header.Finish();

        serializer.WriteInt32(obj.ColorValue);
    }

    public override object Deserialize(in DeserializeInfo info)
    {
        var deserializer = info.Header.Finish();

        var res = new ABColor();
        res.ColorValue = deserializer.ReadInt32();
        return res;
    }
}
    </code></pre>

    <h3 id="serialization-primitives">Full Primitives</h3>
    <hr>
    <p><code>ABSaveSerializer</code> and <code>ABSaveDeserializer</code> have a lot of methods on them, for writing all kinds of primitive things.</p>
    <p>The first thing they provide is a whole load of methods for reading/writing numbers. Here's a list of <i>all</i> the number writing methods available on <code>ABSaveSerializer</code>.</p>
    <p>These methods write all the bytes straight from the numbers given into the output, ensuring they're written in the correct endianness required by the settings.</p>

    <ul>
        <li><p><code>WriteInt16</code></p></li>
        <li><p><code>WriteInt32</code></p></li>
        <li><p><code>WriteInt64</code></p></li>
        <li><p><code>WriteSingle</code></p></li>
        <li><p><code>WriteDouble</code></p></li>
        <li><p><code>WriteDecimal</code> - Writes all 4 bits of the given decimal sequentially with <code>WriteInt32</code></p></li>
    </ul>

    <p>And <code>ABSaveDeserializer</code> has equivalent methods for reading too.</p>
    <p>Then, we have methods for writing strings. These will do everything necessary to represent the string, they'll write the size, they'll encode the string in UTF-8 (or whatever the settings say they want) etc.</p>

    <ul>
        <li><p><code>WriteNonNullString</code></p></li>
        <li><p><code>WriteNullableString</code> - This writes an extra bit <code>WriteNonNullString</code> doesn't have to store whether it's null or not.</p></li>
    </ul>

    <p>And last, but certainly not least, these methods let you directly write raw bytes into the output:</p>

    <ul>
        <li><p><code>WriteByte</code></p></li>
        <li><p><code>WriteRawBytes</code></p></li>
    </ul>

    <p>Just be aware, these two <i>only</i> write the bytes they're given, nothing more. So, if you give <code>WriteBytes</code> a <code>byte[]</code>, it will not <b>NOT</b> write the size of the array or anything like that - it will <i>just</i> directly write the contents of the array. If you want it to be serialized properly like a <code>byte[]</code> would when it's encountered in the object, you'll need to serialize the array with the proper converter, which we'll explain in just a moment.</p>
    <p>There's also <i>one more</i> quite unique method, <code>FastWriteShorts</code>, that takes an array of <code>short</code>s and tries to write them as efficiently as possible, while still ensuring correct endianness for each item. Yet again, this does not write the size.</p>
    <p><code>ABSaveDeserializer</code> has all the same methods in it, so you can use those as the equivalent when reading, just replace <code>Write</code> with <code>Read</code> and that's it.</p>

    <h3 id="serialization-compressed">Compressed Primitives</h3>
    <hr>
    <p>All the number methods above write <i>all</i> of the bytes the number is made up of. For example, <code>WriteInt32</code> writes all 4 bytes of the <code>int</code> you give it.</p>
    <p>However, the value in the integer is often small enough that it doesn't actually need <i>all</i> of those bytes in the output. This is <i>especially</i> the case for sizes on things like arrays and strings, but also applies to most integers. So we are often <b>wasting</b> many bytes on the integer when it doesn't need all of it.</p>
    <p>To solve this, and help us achieve even more compact output, ABSave introduces "compressed" primitive writing methods that you can use to try and write numbers in as few bytes as possible. So, anytime you write an integer, consider writing it with the compressed methods to write it in as few bytes as possible.</p>
    <p>Below is a table with the advantages and disadvantages of using the form, so use that to determine whether it's a good fit or not.

    <table class="docs-table">
        <tr>
			<td><p>Advantages</p></td>
            <td><p>Can save <b>a lot</b> of space, helping write integers in <b>way</b> fewer bytes.</p></td>
		</tr>
        <tr>
			<td><p>Disadvantages</p></td>
            <td>
                <p>Takes a tiny bit more processing to read/write (although this should be minor and the amount work put in does depend on the user's priorities in the <code>ABSaveSettings</code> configuration).</p>
                <p>For <i>very large</i> numbers, it can actually end up taking up <b>more</b> bytes than without.</p>
            </td>
		</tr>
	</table>

    <p>We generally recommend using the compressed form where possible, just be aware of the disadvantages.</p>

    <h3 id="serialization-other">Reading/Writing other</h3>
    <hr>
    <p>So, as you saw, <code>ABSaveSerializer</code> and <code>ABSaveDeserializer</code> have a lot of methods for reading/writing basic primitives. But, what if you don't want to write a basic primitive?</p>
    <p>What if you wanted to read/write a more complex thing, such as say, a <code>Guid</code> or a <code>List&lt;T&gt;</code> in exactly the same way as they're written when they're encountered by ABSave? How do you do that?</p>
    <p>Well, doing this involves two steps.</p>

    <ol>
        <li><p>You need to retrieve a <code>MapItemInfo</code> for the type you're trying to serialize. We'll find out how you can do this in just a moment. This <code>MapItemInfo</code> essentially stores <i>what converter</i> will be used to serialize/deserialize the given type.</p></li>
        <li><p>Once you have that info, you can do then call <code>WriteItem</code> on the <code>ABSaveSerializer</code>, passing in the object you'd like to serialize, as well as the <code>MapItemInfo</code> you retrieved.</p></li>
    </ol>

    <p>The tricky bit is retrieving that <code>MapItemInfo</code>. One way you <i>can</i> do it is by writing this in your serialization code:</p>

    <pre><code class="language-csharp">
MapItemInfo listInfo = serializer.State.GetRuntimeMapItem(typeof(List&lt;string&gt;));
    </code></pre>

    <p>And now, we can give that to the serializer's <code>WriteItem</code> method, and that will serialize it:</p>

    <pre><code class="language-csharp">
serializer.WriteItem(listStringHere, listInfo);
    </code></pre>

    <p>And you can do exactly the same for deserialization.</p>
    <p>However, this is <b>NOT</b> the best way to do it, because <code>GetRuntimeMapItem</code> is <b>slow</b>. You should only do it this way if you don't know what type of item you're going to serialize until you have the actual instance in your hands.</p>
    <p>If you know what type of item you're going to serialize from just the <code>Type</code> on its own, you should get the <code>MapItemInfo</code> in advance. And you can do this using the <code>Initialize</code> method (see the next section)</p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Note</h4>
        <p>There's also a method called <code>WriteNonNullExactItem</code>, which writes an item that is definitely <i>not null</i> and definitely has <i>no inheritance</i> going on (the "exact" part), saving some bits.</p>
    </div>

    <h2 id="initialization">Initialization</h2>
    <hr>
    <p>ABSave provides an overridable method called <code>Initialize</code> on every converter. This method is only called <b>once</b> when the converter is first retrieved for a specific type.</p>
    <p>So, if I have a converter that serializes <code>ABColor</code> and <code>ABGradient</code>. The <code>Initialize</code> method will be called <i>once</i> if the converter is needed for an <code>ABColor</code>, and once if it's needed for an <code>ABGradient</code> somewhere, and that's it, only once per each type.</p>
    <p>This method is a perfect chance for you to pre-prepare as much stuff as you can from purely the type alone, ready for serializing later. Such as getting the <code>MapItemInfo</code> for a certain type once in advance when you know you're going to need it while serializing.</p>
    <p>All the stuff you pre-prepare in this method can be put into the fields on the class, ready to be accessed by the <code>Serialize</code> and <code>Deserialize</code> methods as needed. Each time the converter is used for a different type, a new instance of it is created, so the values in the fields will still be different per type, no need to worry about that.</p>
    <p>So, here's an example of a converter overriding this method, using it to prepare a <code>MapItemInfo</code> for whatever the generic argument is, which is then going to be used in the serialization methods and passed to <code>WriteItem</code>. It would be a waste of time doing this every single time we serialize, and that's why we do it here:</p>

    <pre><code class="language-csharp">
[Select(typeof(ABCollection&lt;&gt;))]
public class ABCollectionConverter : Converter
{
    // This can be checked in the serialize and deserialize methods.
    MapItemInfo _itemConverter;

    public override uint Initialize(InitializeInfo info)
    {
        // You can use "info.GetMap" to get the "MapItemInfo" for a certain type - doing it here is much better than doing everytime we it serialize.
        _itemConverter = info.GetMap(info.Type.GetGenericArguments()[0]);

        // Ignore the return value for now
        return 0;
    }

    // Serialize logic here
}
    </code></pre>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Tip</h4>
        <p>You are <i>also</i> allowed to modify fields in <code>CheckType</code> too, which may be more convenient than in <code>Initialize</code>.</p>
        <p><b>However</b>, you must be careful not to change any fields if your <code>CheckType</code> is going to return <code>false</code>. Because of how ABSave pools converters that return <code>false</code> from <code>CheckType</code>, you risk a lot of painful bugs if you change the fields when returning <code>false</code>. Also do remember <code>CheckType</code> won't even run for things <code>Select</code>ed from the attributes.</p>
    </div>

    <h2 id="serialization-header">The Header</h2>
    <hr>
    <p>You know what the <b>best</b>, most powerful part of ABSave is? You know what makes it win in size over almost every other format? No, it's not the compressed writing form, nor the lack of names. It's the <b>carry over bit system</b>. What is that? Only the best part of ABSave, and <i>definitely</i> something you <b>should</b> be taking advantage of in your converter! And this is where <code>info.Header</code> comes in.</p>
    <p>When ABSave serializes a reference type object, it often write some extra bits to store whether it's null or whether it has any inheritance activity occurring. Like so:</p>

    <pre><code>
1 1 x x x x x x
^ Is Not Null
  ^ Has Matching Inheritance Type
    </code></pre>

    <p>As you can see, the first two bits of the byte are used for that info. But... What happens to those remaining <code>x</code>s of the byte? ABSave isn't using them for anything, they're just sitting there. And in some other serializers those bits <i>could</i> go to waste, but for ABSave, we don't want to waste bits like that.</p>
    <p>So, the <code>info.Header</code> lets you write data into those <code>x</code>s, and take advantage of them if you can. If you look at <code>info.Header</code>, you'll notice it's a <code>BitWriter</code>. This type writes to the output similar to <code>ABSaveSerializer</code>, except it writes bit-by-bit, and not in whole bytes. You'll notice it contains the following methods:</p>

    <table class="docs-table">
        <tr>
			<th><p>Method</p></th>
            <th><p>Description</p></th>
		</tr>
        <tr>
			<td><p>WriteBitOff</p></td>
            <td><p>Writes <code>0</code> into the next bit.</p></td>
		</tr>
        <tr>
            <td><p>WriteBitOn</p></td>
            <td><p>Writes <code>1</code> into the next bit.</p></td>
		</tr>
        <tr>
            <td><p>WriteBitWith(bool)</p></td>
            <td><p>Writes <code>0</code> into the next bit if given <code>false</code>, <code>1</code> if not.</p></td>
		</tr>
        <tr>
            <td><p>WriteInteger(byte, byte)</p></td>
            <td><p>Writes the least significant bits of the given byte across the number of bits specified. E.g. <code>WriteInteger(4, 3)</code>, will write <code>100</code> in the next three bits, and move on.</p></td>
		</tr>
	</table>

    <p>These are quite self-explanatory, hopefully. These are what you use to write bit-by-bit. And don't worry about filling up all the "x"s - it will automatically overflow onto a new byte if you write beyond, so you just don't need to worry about it!</p>
    <p>And when deserializing, the <code>BitReader</code> has equivalent methods just like these for reading from the header.</p>
    <p>However, these aren't the only methods - on both <code>BitWriter</code> and <code>BitReader</code>, you'll <i>also</i> find methods like these, exactly like some of the methods you see on <code>ABSaveSerializer</code>:</p>

    <ul>
        <li><p><code>WriteNonNullString</code></p></li>
        <li><p><code>WriteNullableString</code></p></li>
        <li><p><code>WriteItem</code></p></li>
        <li><p><code>WriteExactNonNullItem</code></p></li>
        <li><p><code>WriteCompressedInt</code></p></li>
        <li><p><code>WriteCompressedLong</code></p></li>
    </ul>

    <p>The difference between calling them on the <code>ABSaveSerializer</code> and the <code>BitWriter</code> is when you call them on the <code>BitWriter</code>, they will use up the left over <code>x</code>s to store their own data.</p>
    <p>For example, when you call <code>WriteNonNullString</code>, it needs to write the size of the string to the output. If you call it on the <code>ABSaveSerializer</code>, it will create a whole new byte to try and hold that size. But with <code>BitWriter</code>, it will use the left over <code>x</code>s you have to write the size, which can be <b>much more</b> efficient.</p>
    <p>Another example, <code>WriteCompressedInt</code>. Calling this on the serializer object will create a whole new byte to try hold the compressed form, but on the bit writer it will <i>pack</i> as much of the compressed form as possible (maybe even all of it!) into the <code>x</code>s you have left, which is, again, more efficient.</p>
    <p>In general, you should try and do <b>as much as possible</b> from the <code>BitWriter</code>, it will benefit you greatly to just keep on using it if you can. And don't forget that it overflows, so you can keep on calling these methods above again and again no problem. The <i>only reason</i> you should move over to the <code>ABSaveSerializer</code> is if you <i>need to</i> to access some method that's <i>not</i> available on <code>BitWriter</code>, like <code>WriteByte</code> or <code>WriteInt32</code>, see the next section:</p>
    
    <h3 id="serialization-header-finishing">When to Finish with the BitWriter</h3>
    <hr>
    <p>When you're using <code>BitWriter</code>, there is <b>one</b> rule you must ensure you follow:</p>

    <ul>
        <li><p>You <b>must not</b> write to the <code>ABSaveSerializer</code> without calling <code>Finish</code> on the <code>BitWriter</code> first.</p></li>
    </ul>

    <p>This is the <b>only</b> situation you need to call <code>Finish</code> on the header and stop using it, only <i>if</i> you need to use the <code>ABSaveSerializer</code> for something exclusive to it, like <code>WriteInt32</code> or <code>WriteByte</code>. If you don't need the <code>ABSaveSerializer</code> and can do everything with the <code>BitWriter</code>, then you <b>should not</b> call <code>Finish</code>!</p>
    <p>It will actually be much better if you don't call it, because not calling <code>Finish</code> leaves the header open, and available for the next thing ABSave serializes to use! For example, imagine this was the <code>Serialize</code> method in a converter for <code>ABBoolean</code>:</p>

    <pre><code class="language-csharp">
var abBoolean = (ABBoolean)info.Instance;
writer.WriteBitWith(abBoolean.BooleanValue);
    </code></pre>

    <p>That could be your entire Serialize method! Now, let's imagine you had a <code>ABBoolean[]</code> with 5 items in it. Guess what's going to happen! Because we kindly left the header open for whatever comes after us to use, all of the <code>ABBoolean</code>s in the array are literally just going to share exactly the same header, like so:</p>

    <pre><code>
...0 1 1 0 0
   ^ First ABBoolean
     ^ Second ABBoolean
       ^ Third ABBoolean
          ^ Fourth ABBoolean
            ^ Fifth ABBoolean
    </code></pre>

    <p>Now <i>that's</i> what you call compact, just from not finishing the header when you don't need to, you've left it available to be used by the next thing. And this doesn't just happen on arrays or anything, it will happen just about everywhere, the member after you in an object will be able to use the same header etc.</p>

    <h3 id="serialization-header-finishing">Using BitWriter after Finishing</h3>
    <hr>
    <p>Let's say you used the <code>BitWriter</code>, you <code>Finish</code>ed with it and used the <code>ABSaveSerializer</code> to write some things. And then, you realize you'd really like to start doing some bit-by-bit writing again, just writing bit-by-bit into a new byte in the middle of your serialization. It would be really nice to have a <code>BitWriter</code> again to do that bit writing for you, wouldn't it?</p>
    <p>Well, don't worry - because you can do exactly that! You are <i>allowed</i> to reuse the <code>info.Header</code> after you <code>Finish</code> with it, allowing you to do more bit-by-bit writing if you'd like after you've written loads of bytes.</p>
    <p><b>However</b>, <i>as soon as</i> you write anything to it, you yet again need to abide by the rule above that you <b>can't</b> use the <code>ABSaveSerializer</code> until you finish with it. And similarly, if you <i>don't</i> need to use <code>ABSaveSerializer</code> after it, you should <i>not</i> Finish it! That way you can leave that byte you were writing to open for things following your converter to use.</p>

    <div class="msgBox infoBox">
        <h4 class="noAnchor">Tip</h4>
        <p>To stop yourself from slipping up and using the <code>ABSaveSerializer</code> before you <code>Finish</code> with the header, you <i>could</i> make a new method in your converter, and pass it <b>only</b> the <code>BitWriter</code>.</p>
        <p>Now because since all it has to access is the bit writer, it <i>has to</i> to call <code>Finish</code> to get a hold of the serializer, stopping you from slipping up.</p>
    </div>

    <p><b>TO BE CONTINUED</b></p>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>