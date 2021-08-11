<!DOCTYPE html>
<html>

<head>
    <title>Settings</title>
    <?php include('../../base/pageHeader.html') ?>
</head>

<body>
    <?php include('../../base/pageBodyStart.html') ?>
    
    <h1 id="title">Settings</h1>
    <hr>
    <p>As you may have noticed in the quick start, there's an object called <code>ABSaveSettings</code> you use when creating a new map. This object allows you customize how the serialization process happens, to quite a fine level.</p>
    <p>There are four presets already provided: <code>ForSpeed</code>, <code>ForSize</code>, <code>ForSpeedVersioned</code> and <code>ForSizeVersioned</code>.</p>
    <p>A <b>preset</b> is just a settings object that comes with a lot of its settings already <i>pre-set</i>. The only difference between <code>Versioned</code> and non-<code>Versioned</code> presets is whether the <code>IncludeVersioning</code> setting is enabled.</p>
    <p>Below you will find how you can customize the settings, as well as a table showing all the differet settings and when you would want one or the other.</p>

    <h2 id="customizing">Customizing Settings</h2>
    <hr>
    <p>To customize the settings, first choose a preset you'd like to use as a base. What we're going to do is take that preset you choose as a base, and then change some of the settings on it to what we'd prefer, leaving the rest as they were in the preset.</p>
    <p>Once you've chosen your base preset, simply take that preset and call <code>Customize</code> on it, passing in a lambda, as seen below:</p>

    <pre><code class="language-csharp">
ABSaveSettings.ForSpeed.Customize(s => ...);
    </code></pre>

    <p>The object in that <code>s</code> there contains a group of methods, simply call these to change some of the settings. For example, if you there was a hypothetical setting called <code>A</code>, you can change it like so:</p>

    <pre><code class="language-csharp">
ABSaveSettings.ForSpeed.Customize(s => s.SetA(false));
    </code></pre>

    <p>You can also continually chain method calls here to change multiple settings. For example, this sets hypothetical setting <code>A</code> to <code>false</code>, and well as the setting <code>B</code> to <code>"abc"</code>.

    <pre><code class="language-csharp">
ABSaveSettings.ForSpeed.Customize(s => s.SetA(false).SetB("abc"));
    </code></pre>

    <h2 id="all-settings">All Settings</h2>
    <hr>
    <p>Now you know <i>how</i> to change them, below is a table of all the settings, and what effect changing them might have. Also included is the default values in the presets.</p>
    <p>Please note that <i>with the exception of <code>IncludeVersioning</code></i> changing <i>any</i> of the following settings will break reading previous output from former settings.</p>

    <table class="docs-table">
		<tr>
			<th>Name</th>
			<th>Description</th>
			<th><code>ForSpeed</code></th>
            <th><code>ForSize</code></th>
		</tr>
		<tr>
			<td><p><code>UseUTF8</code></p></td>
			<td>
                <p>If <code>true</code>, ABSave should prefer UTF-8 when writing text. If <code>false</code>, ABSave should prefer UTF-16. It is recommended to keep this on <code>true</code>, as UTF-16 often makes an absolutely <i>huge</i> impact on the output size.</p>
                <p>However, if you really don't care about output size, switching this to UTF-16 <i>will</i> in most cases speed things up. <b>That said, if you have a lot of <i>foreign</i> characters in your classes, this may improve output size on those</b></p>
            </td>
			<td><p>True</p></td>
            <td><p>True</p></td>
		</tr>
		<tr>
			<td><p><code>UseLittleEndian</code></p></td>
			<td>
                <p>If <code>true</code>, ABSave should prefer little endian when writing numbers, if not prefer big endian. This is the default because a lot of systems, including almost every desktop machine, are little endian, making it a more efficient form for these machines to deal with.</p>
                <p>Setting this to <code>false</code> should improve performance on any big endian systems involved, but will impact performance on little endian. It's rare big endian systems are involved, but if they are and they are a priority performance-wise, switching this to <code>false</code> may be a good idea.</p>
            </td>
			<td><p>True</p></td>
            <td><p>True</p></td>
		</tr>
        <tr>
			<td><p><code>LazyCompressedWriting</code></p></td>
			<td>
                <p>While serializing, ABSave has its own format for storing numbers in a few bytes as possible, called the <b>compressed</b> format. (See the next setting in the table for when this form is used).</p>
                <p>When <code>false</code>, ABSave should prioritize trying to pack the number into as few bits as possible when writing this compressed format. This is at the cost of performance. When <code>true</code>, ABSave will be "lazy" with this form and try to write it as quickly as possible, resulting in a less efficient form used.</p>
            </td>
			<td><p>True</p></td>
            <td><p>False</p></td>
		</tr>
        <tr>
			<td><p><code>CompressPrimitives</code></p></td>
			<td>
                <p>When <code>true</code>, ABSave should prefer writing all primitive numbers in the compressed form where possible, to try and compact the output as much as possible. This can have an impact on performance.</p>
                <p>When <code>false</code>, ABSave will not write primitive numbers in the compressed form, and will instead keep its use to places where it is explicitly called upon, which is almost always for storing the sizes of things, such as array or string lengths.</p>
                <p><b>NOTE: If you have <i>a lot</i> of very, very large numbers, disabling this may actually <i>reduce</i> the output size, as the compressed form can be less efficient for very large numbers.</b></p>
            </td>
			<td><p>False</p></td>
            <td><p>True</p></td>
		</tr>
        <tr>
			<td><p><code>IncludeVersioning</code></p></td>
			<td>
                <p>When <code>true</code>, ABSave will include some extra bits in the output to store versioning. When <code>false</code>, no versioning info is stored, and <b>version '0'</b> of everything is serialized.</p>                
                <p><b>This is the only setting that can be changed without affecting the output, as there's a <i>header</i> at the beginning of each document denoting whether version numbers are present or not.</b></p>
            </td>
			<td><p>Varies</p></td>
            <td><p>Varies</p></td>
		</tr>
        <tr>
			<td><p><code>IncludeVersionHeader</code></p></td>
			<td>
                <p>When <code>true</code>, ABSave will include a header at the very beginning of a document, stating whether <code>IncludeVersioning</code>. This allows enabling the setting later on down the line without breaking anything. This header literally takes up <b>one bit</b>, and it's generally suggested to just leave it on as it's not worth disabling.</p>
            </td>
			<td><p>True</p></td>
            <td><p>True</p></td>
		</tr>
	</table>

    <?php include('../../base/pageBodyEnd.html') ?>
</body>
</html>