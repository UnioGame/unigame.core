# Unity Tools

UniTools core package

# LifeTime

LifeTime is a comprehensive resource management system that provides automatic cleanup, hierarchical lifetime management, and async operation cancellation support. It's designed to eliminate memory leaks and simplify resource management in Unity applications.

## Core Components

### LifeTime
The main lifetime implementation that handles resource tracking and cleanup. Provides methods like `Terminate()`, `Release()`, and `Restart()`.

### AssetLifeTime
Automatic lifetime management for Unity objects (GameObjects, Components) tied to their destruction.

### SceneLifeTime
Scene-based lifetime management that automatically cleans up when scenes are unloaded.

## Basic Usage

```csharp
// Create a lifetime
var lifeTime = new LifeTime();
// Or use factory method
var lifeTime = LifeTime.Create();

// Add cleanup actions - executed in reverse order when terminated
lifeTime.AddCleanUpAction(() => Debug.Log("First cleanup"));
lifeTime.AddCleanUpAction(() => Debug.Log("Second cleanup")); // Executes first

// Add disposable objects for automatic disposal
lifeTime.AddDispose(someDisposableObject);

// Keep object references to prevent garbage collection
lifeTime.AddRef(heavyObject);

// Check lifetime state
if (lifeTime.IsTerminated) return;
if (lifeTime.IsAlive) { /* do work */ }

// Terminate lifetime (triggers all cleanup actions)
lifeTime.Terminate(); // Same as Release()

// Alternative: Release for cleanup
lifeTime.Release(); // Clears all references and terminates

// Restart lifetime (clears and resets for reuse)
lifeTime.Restart(); // Calls Release() then sets IsTerminated = false
```

## Asset-Based LifeTime

```csharp
// Get lifetime tied to GameObject lifecycle
var gameObjectLifeTime = myGameObject.GetAssetLifeTime();

// Automatically cleanup when GameObject is destroyed
gameObjectLifeTime.AddCleanUpAction(() => Debug.Log("GameObject destroyed"));

// With terminate on disable option
var disableLifeTime = myGameObject.GetAssetLifeTime(terminateOnDisable: true);

// Extension methods for automatic destruction
someObject.DestroyWith(lifeTime);        // Destroy Unity object when lifetime ends
component.DespawnWith(lifeTime);         // Return to object pool when lifetime ends
```

## Scene-Based LifeTime

```csharp
// Get lifetime for current active scene
var sceneLifeTime = SceneLifeTime.GetActiveSceneLifeTime();

// Get lifetime for specific scene
var specificSceneLifeTime = myScene.GetSceneLifeTime();

// Add disposable to scene lifetime
myDisposable.AddToActiveScene();
myDisposable.AddTo(myScene);

// Automatic cleanup when scene unloads
sceneLifeTime.AddCleanUpAction(() => Debug.Log("Scene unloaded"));
```

## Async Operations & CancellationToken

```csharp
// Convert to CancellationToken for async operations
var lifeTime = new LifeTime();
var cancellationToken = lifeTime.Token;

// Use with async methods
await SomeAsyncOperation(cancellationToken);

// Or direct conversion
await SomeAsyncOperation(lifeTime); // Implicit conversion

// Timeout with logging
await lifeTime.AwaitTimeoutLog(
    timeout: TimeSpan.FromSeconds(5),
    message: () => "Operation timed out!",
    logType: LogType.Warning
);
```

## Hierarchical Lifetime Management

```csharp
// Parent-child relationships
var parentLifeTime = new LifeTime();
var childLifeTime = new LifeTime();

// Child terminates when parent terminates
childLifeTime.AddTo(parentLifeTime);

// Child releases (resets) when parent terminates
childLifeTime.ReleaseWith(parentLifeTime);

// Direct child lifetime management
parentLifeTime.AddChildLifeTime(childLifeTime); // Child terminates with parent
parentLifeTime.AddChildRestartLifeTime(childLifeTime); // Child restarts with parent

// Multiple child lifetimes
var children = new[] { child1, child2, child3 };
foreach(var child in children)
    child.AddTo(parentLifeTime);
```

## Lifetime Composition

### Union LifeTime
Terminates when ALL composed lifetimes terminate:

```csharp
// Create union - terminates when ALL lifetimes terminate
var unionLifeTime = lifeTime1.MergeLifeTime(lifeTime2, lifeTime3);

// With cleanup action
var unionWithCleanup = lifeTime1.MergeLifeTime(
    cleanup: () => Debug.Log("All lifetimes terminated"),
    lifeTime2, lifeTime3
);

// Manual union creation
var union = new UnionLifeTime();
union.Add(lifeTime1, lifeTime2, lifeTime3);
```

### Composed LifeTime  
Terminates when ANY composed lifetime terminates:

```csharp
// Create composition - terminates when ANY lifetime terminates
var composedLifeTime = lifeTime1.Compose(lifeTime2, lifeTime3);

// With cleanup action
var composedWithCleanup = lifeTime1.ComposeCleanUp(
    cleanup: () => Debug.Log("First lifetime terminated"),
    lifeTime2, lifeTime3
);
```

## MonoBehaviour Integration

```csharp
public class MyComponent : MonoBehaviour, ILifeTimeContext
{
    private LifeTime _lifeTime = new();
    
    public ILifeTime LifeTime => _lifeTime;
    
    private void Start()
    {
        // Setup cleanup when component is destroyed
        _lifeTime.AddCleanUpAction(() => Debug.Log("Component cleanup"));
        
        // Subscribe to events with automatic unsubscribe
        EventSystem.Subscribe<MyEvent>(OnMyEvent).AddTo(_lifeTime);
    }
    
    private void OnDestroy()
    {
        _lifeTime.Terminate();
    }
    
    private void OnMyEvent(MyEvent evt) { /* Handle event */ }
}

// Or use LifeTimeBehaviour for automatic management
public class AutoLifetimeComponent : LifeTimeBehaviour
{
    private void Start()
    {
        // Automatically cleaned up on destroy
        AddCleanUpAction(() => Debug.Log("Auto cleanup"));
        
        // Access disable lifetime for OnDisable cleanup
        DisableLifeTime.AddCleanUpAction(() => Debug.Log("Disabled"));
    }
}
```

## Advanced Patterns

### Conditional Cleanup

```csharp
// Execute action and add cleanup cancellation
lifeTime.AddTo(
    action: () => StartSomeProcess(),
    cancellationAction: () => StopSomeProcess()
);

// Disposable action pattern
var disposableAction = ClassPool.Spawn<DisposableAction>();
disposableAction.Initialize(() => Debug.Log("Disposed"));
lifeTime.AddDispose(disposableAction);
```

### Performance Optimizations

```csharp
// Check if lifetime is terminated before expensive operations
if (lifeTime.IsTerminatedLifeTime()) 
    return;

// Use static terminated lifetime for null/invalid cases
var safeLifeTime = someObject?.GetLifeTime() ?? LifeTime.TerminatedLifetime;

// Editor lifetime for tools that persist between play mode changes
var editorLifeTime = LifeTime.EditorLifeTime;

// Factory method for clean creation
var newLifeTime = LifeTime.Create();

// Direct state checks
if (lifeTime.IsAlive) { /* perform operations */ }
if (lifeTime.IsTerminated) { /* skip operations */ }
```

### Extension Methods

```csharp
// Get lifetime from any object
var objectLifeTime = someObject.GetLifeTime(); // Works with GameObject, Component, Scene, etc.

// Fluent API
someDisposable
    .AddTo(lifeTime)
    .DestroyWith(anotherLifeTime);

// Create lifetime commands
var command = ((Action<ILifeTime>)(lt => Setup(lt))).CreateLifeTimeCommand();
```

## Best Practices

1. **Always terminate lifetimes**: Use `OnDestroy`, `OnApplicationQuit`, or similar to ensure cleanup
2. **Use appropriate lifetime scope**: Match lifetime to object lifecycle (Scene, GameObject, Component)
3. **Prefer composition over inheritance**: Use `ILifeTimeContext` interface rather than inheriting from lifetime classes
4. **Check termination state**: Before expensive operations, check `IsTerminated` property
5. **Use extension methods**: Leverage built-in extensions for common patterns
6. **Hierarchical cleanup**: Structure lifetimes to match your object hierarchy

## Features

- **Automatic Resource Management**: Automatically dispose of objects and execute cleanup actions
- **CancellationToken Support**: Seamless integration with async/await patterns
- **Hierarchical Management**: Parent-child lifetime relationships with automatic cleanup
- **Scene Integration**: Automatic cleanup when scenes unload
- **Asset Integration**: Automatic cleanup when Unity objects are destroyed
- **Thread-Safe Operations**: Safe to use across multiple threads
- **Performance Optimized**: Minimal allocation overhead with object pooling
- **Composition Patterns**: Union and composed lifetimes for complex scenarios
- **Editor Support**: Special handling for editor workflows and play mode changes

# UrlChecker

UrlChecker provides functionality to test multiple endpoints and find the fastest available one.

## Usage

```csharp
// Test multiple URLs and get the fastest one
string[] urls = { "https://api1.example.com", "https://api2.example.com" };

// Select fastest endpoint
var fastestUrl = await UrlChecker.SelectFastestEndPoint(urls, timeout: 5);
Debug.Log($"Fastest URL: {fastestUrl.url}, Time: {fastestUrl.time}ms");

// Check all endpoints
var results = await UrlChecker.CheckEndPoints(urls, timeout: 5);
Debug.Log($"Best endpoint: {results.bestResult.url}");

// Test single URL
var result = await UrlChecker.TestUrlAsync("https://api.example.com", timeout: 5);
Debug.Log($"URL status: {result.success}, Time: {result.time}ms");
```

## Features

- **Performance Testing**: Measure response times for different endpoints
- **Automatic Selection**: Find the fastest available endpoint
- **Timeout Support**: Configure request timeouts
- **Batch Testing**: Test multiple URLs simultaneously

# WebRequestBuilder

WebRequestBuilder is a comprehensive HTTP client for Unity with support for GET, POST, PATCH requests, authentication, and media downloads.

## Basic Usage

```csharp
var builder = new WebRequestBuilder();

// Simple GET request
var response = await builder.GetAsync("https://api.example.com/data");
if (response.success)
{
    Debug.Log(response.data);
}

// POST request with JSON data
var jsonData = "{\"name\":\"test\"}";
var postResponse = await builder.PostAsync(
    url: "https://api.example.com/users",
    data: jsonData,
    headers: new Dictionary<string, string> { ["Content-Type"] = "application/json" }
);

// PATCH request
var patchResponse = await builder.PatchAsync(
    url: "https://api.example.com/users/123",
    data: jsonData
);
```

## Advanced Features

### Authentication

```csharp
// Set bearer token for authentication
builder.SetToken("your-auth-token");

// Now all requests will include Authorization header
var response = await builder.GetAsync("https://api.example.com/protected");
```

### Parameters and Headers

```csharp
var parameters = new Dictionary<string, string>
{
    ["page"] = "1",
    ["limit"] = "10"
};

var headers = new Dictionary<string, string>
{
    ["Accept"] = "application/json",
    ["User-Agent"] = "MyApp/1.0"
};

var response = await builder.GetAsync(
    url: "https://api.example.com/data",
    parameters: parameters,
    headers: headers,
    timeout: 30
);
```

### Media Downloads

```csharp
// Download texture
var textureResult = await builder.GetTextureAsync("https://example.com/image.jpg");
if (textureResult.success)
{
    myRenderer.material.mainTexture = textureResult.texture;
}

// Download sprite
var spriteResult = await builder.GetSpriteAsync("https://example.com/icon.png");
if (spriteResult.success)
{
    myImage.sprite = spriteResult.sprite;
}
```

### Form Data and File Uploads

```csharp
// POST with form data
var form = new WWWForm();
form.AddField("username", "john");
form.AddField("email", "john@example.com");

var response = await builder.PostAsync("https://api.example.com/register", form);

// POST with binary data
byte[] fileData = File.ReadAllBytes("path/to/file.png");
var uploadResponse = await builder.PostAsync(
    url: "https://api.example.com/upload",
    headers: new Dictionary<string, string> { ["Content-Type"] = "application/octet-stream" },
    data: fileData
);
```

## Configuration

```csharp
var builder = new WebRequestBuilder();

// Enable/disable automatic version parameter
builder.addVersion = true; // Adds "v" parameter with Application.version

// Set default authentication token
builder.SetToken("default-token");

// Build custom requests
var request = builder.BuildGetRequest(
    url: "https://api.example.com",
    parameters: parameters,
    headers: headers,
    timeout: 30
);
```

## Response Handling

```csharp
var response = await builder.GetAsync("https://api.example.com/data");

// Check response status
if (response.success)
{
    Debug.Log($"Success: {response.data}");
}
else
{
    Debug.LogError($"Error: {response.error}");
    Debug.LogError($"HTTP Code: {response.responseCode}");
    
    if (response.networkError)
        Debug.LogError("Network error occurred");
    
    if (response.httpError)
        Debug.LogError("HTTP protocol error");
}
```

# ObjectPool

ObjectPool provides high-performance object pooling for GameObjects and regular classes to reduce memory allocations and improve performance.

## GameObject Pooling

```csharp
// Spawn GameObjects from pool
var pooledObject = ObjectPool.Spawn(prefab, position, rotation);

// Spawn with specific settings
var activeObject = ObjectPool.Spawn(prefab, position, rotation, parent: transform, stayWorld: false);

// Spawn multiple objects asynchronously
var spawnResult = await ObjectPool.SpawnAsync(prefab, count: 10, position, rotation, parent: transform);

// Return objects to pool
ObjectPool.Despawn(pooledObject);

// Create pool with preloaded objects
ObjectPool.CreatePool(prefab, preloads: 50);

// Attach pool to specific lifetime
ObjectPool.AttachToLifeTime(prefab, lifeTime, createIfEmpty: true, preload: 10);
```

## Class Pooling

```csharp
// Spawn class instances from pool
var instance = ClassPool.Spawn<MyClass>();

// Spawn with initialization
var instance = ClassPool.Spawn<MyClass>(item => {
    item.Initialize();
    item.SetActive(true);
});

// Return to pool
instance.Despawn();

// Or using extensions
ClassPool.Despawn(instance);
```

## Features

- **High Performance**: Reduces garbage collection pressure
- **Async Support**: Asynchronous spawning for large batches
- **Lifetime Integration**: Automatic cleanup with lifetime management
- **Preloading**: Preload objects for instant availability
- **Flexible**: Works with GameObjects and regular classes

# ProfilerTools

ProfilerTools provide performance monitoring and profiling utilities for measuring execution time and memory usage.

## Basic Profiling

```csharp
// Start timing
var profilerId = ProfilerUtils.BeginWatch("MyOperation");

// Your code here
PerformSomeOperation();

// Stop and get results
var result = ProfilerUtils.StopWatch(profilerId);
Debug.Log($"Operation took: {result.watchMs} ms");
```

## GameProfiler

```csharp
// Start profiling
var watchId = GameProfiler.BeginWatch("Database Query");

// Your code
await DatabaseQuery();

// Stop profiling (automatically logs results)
GameProfiler.StopWatch(watchId);

// Memory profiling
GameProfiler.BeginMemorySample("Level Loading");
LoadLevel();
GameProfiler.EndMemorySample(); // Logs memory usage

// Unity Profiler integration
GameProfiler.BeginSample("Custom Sample");
CustomOperation();
GameProfiler.EndSample();
```

## Features

- **High-Resolution Timing**: Precise performance measurements
- **Memory Profiling**: Track memory allocations
- **Unity Integration**: Works with Unity Profiler
- **Editor Controls**: Enable/disable profiling in editor
- **Automatic Logging**: Built-in result reporting

# Extension Methods

UniGame.Core provides numerous extension methods to simplify common operations.

## Collection Extensions

```csharp
// Get random values
var randomItem = myList.GetRandomValue();
var randomItems = myList.GetRandomValues(count: 3);

// Safe operations
var randomWithCondition = myList.GetRandomValue(x => x.IsActive);

// Dictionary utilities
myDictionary.RemoveWithValue(someValue);
myDictionary.RemoveAll((key, value) => value.IsExpired());

// LINQ enhancements
var chunks = myList.ChunkBy(10); // Split into chunks of 10
var enhanced = items.Examine(x => Debug.Log(x.name)); // Side effects in LINQ
```

## Math Extensions

```csharp
// Simplified math operations
var absolute = (-5.5f).Abs(); // 5.5f
var clamped = value.Clamp(0f, 100f);
var rounded = someFloat.ToStringRoundToInt(); // Convert to int string
```

## Vector Extensions

```csharp
// Vector utilities
var isLeft = vectorA.IsLeft(vectorB);
var worldOffset = screenOffset.GetWorldOffsetFromScreenOffset(camera);
var distance = componentA.Distance(componentB);

// Vector modifications
var rotated = vector.Rotate902D(); // Rotate 90 degrees in 2D
var reflected = point.Reflect(source, reflectionLine);
```

## Unity Object Extensions

```csharp
// Component utilities
var rectTransform = myMonoBehaviour.RectTransform();
var rootAsset = myComponent.GetRootAsset();

// Lifetime integration
gameObject.DespawnWith(lifeTime); // Auto-despawn with lifetime
component.DestroyWith(lifeTime);  // Auto-destroy with lifetime
```

## JSON Extensions

```csharp
// Easy JSON serialization
var json = myObject.ToJson();
var jsonArray = myArray.ToJson();

// Deserialization
var obj = jsonString.FromJson<MyClass>();
var array = jsonString.FromArrayJson<MyClass>();
```

# Custom Attributes

UniGame.Core includes various attributes for inspector customization and editor enhancement.

## Inspector Attributes

```csharp
public class MyComponent : MonoBehaviour
{
    [AssetFilter(typeof(AudioClip), folderFilter: "Audio/Music")]
    public AudioClip backgroundMusic;
    
    [Reorderable("Audio Clips", enableAdd: true, enableDelete: true)]
    public List<AudioClip> soundEffects;
    
    [ReadOnlyValue]
    public float calculatedValue;
    
    [Layer]
    public int targetLayer;
    
    [SortingLayer]
    public int sortingLayer;
}
```

## Field Type Attributes

```csharp
[AssetTypeFilter(typeof(Texture2D))]
public Object textureField;

[FieldTypeDrawer(typeof(MyCustomType))]
public class MyCustomDrawer : PropertyDrawer
{
    // Custom drawer implementation
}
```

## Features

- **Asset Filtering**: Filter asset selection by type and folder
- **Reorderable Lists**: Enhanced list/array editing
- **Read-Only Fields**: Display-only inspector fields
- **Layer Selection**: Unity layer and sorting layer pickers
- **Custom Drawers**: Framework for custom property drawers

# Utility Classes

UniGame.Core provides various utility classes for common operations.

## File Utilities

```csharp
// File operations
var (path, content) = FileUtils.ReadContent("Assets/config.json");
FileUtils.WriteAssetsContent("Assets/generated.cs", sourceCode);

// Path utilities
var projectPath = FileUtils.ProjectPath;
var fixedPath = somePath.FixUnityPath();
var isFile = path.IsFilePath();

// Directory operations
FileUtils.CreateDirectories(targetPath, isFilePath: true);
```

## Memory and Caching

```csharp
// Memorization for expensive operations
var cachedFunction = MemorizeTool.Create<string, ProcessedData>(ProcessData);
var result = cachedFunction("input"); // First call processes, subsequent calls return cached result

// Memory management
await MemoryUtils.CleanUpFullMemoryAsync();
MemoryUtils.CleanUp(); // Force garbage collection
```

## Type Utilities

```csharp
// Type string caching
var stringValue = myInt.ToStringFromCache(); // Cached string conversion
var enumString = myEnum.ToStringFromCache(); // Cached enum names

// Type analysis
var typeIds = myType.GetTypeIds();
var typeNames = myType.GetTypeNames();
```

## Streaming Assets

```csharp
// Load data from StreamingAssets
var result = await StreamingAssetsUtils.LoadDataFromWeb<MyData>("config.json");
if (result.success)
{
    var data = result.data;
}

// Save data (Editor only)
StreamingAssetsUtils.SaveToStreamingAssets("output.json", myData);
```

# Game Logger

** Installing the https://github.com/Cysharp/ZString can be useful for better performance of the logger. **

if ZString is installed in the project on with the package manager, when add define ENABLE_ZSTRING to the project, the logger will use ZString for better performance.

```
    "versionDefines": [
        {
            "name": "com.cysharp.zstring",
            "expression": "",
            "define": "ENABLE_ZSTRING"
        }
    ],
```

 
```csharp
GameLog.Log("Some Log Message") //pring logs to unity console under Editor, this method is not used in build
    
GameLog.Log("Some Log Message",Color.blue) //print logs with color to unity console under Editor, this method is not used in build

GameLog.LogRuntime("Some Log Message") //print logs to unity console under Editor and in build

GameLog.LogError("Some Error Message") //print error logs to unity console and send to firebase crashlytics if define is enabled

GameLog.LogException(Exception e) //print exception logs to unity console and send to firebase crashlytics if define is enabled
```

## Firebase Crashlytics Support

```csharp
ENABLE_FIREBASE_CRASHLYTICS //define for enabling crashlytics support
```

```csharp
GameLog.LogError("Some Error Message") //send error logs to firebase crashlytics if define is enabled
```

# Editor Drawers

you can see a demo asset with all drawers with menu:

```csharp
[CreateAssetMenu(menuName = "UniGame/Examples/Drawers/DemoDrawersAsset", fileName = "DemoDrawersAsset")]
public class UniDrawersDemoAsset : ScriptableObject
```

## Value Dropdown

![value-dropdown](https://i.ibb.co/zT1q3KCY/dr1.png)


```csharp
    [ValueDropdown(nameof(GetDropdownValues))]
    public string valueDropdown = "Default Value";
    
    public IEnumerable<string> GetDropdownValues()
    {
        yield return "Default Value";
        yield return "Value 1";
        yield return "Value 2";
    }
    
    [ValueDropdown(nameof(GetStaticDropdownValues))]
    public string valueStaticDropdown = "Default Value";

    public static IEnumerable<string> GetStaticDropdownValues()
    {
        yield return "STATIC 1";
        yield return "STATIC 1";
        yield return "STATIC 12";
    }
    
    [ValueDropdown(nameof(GetDropdownIntValues))]
    public string valueIntDropdown = "NONE";

    public IEnumerable<int> GetDropdownIntValues()
    {
        yield return 1;
        yield return 55;
        yield return 66;
    }
    
    [ValueDropdown(nameof(GetDropdownIntValues),label:"label")]
    public string valueLabelDropdown = "NONE";
    
    public IEnumerable<DemoLabelValue> GetDropdownLabelValues()
    {
        yield return new DemoLabelValue(){ value = "", label = "label1"};
        yield return new DemoLabelValue(){ value = "", label = "label2"};
        yield return new DemoLabelValue(){ value = "", label = "label3"};
    }
```