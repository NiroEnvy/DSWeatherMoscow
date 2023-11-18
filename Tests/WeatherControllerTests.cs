namespace Tests;

public class WeatherControllerTests
{
    private readonly WeatherController _controller;
    private readonly WeatherDbContext _mockContext;
    private readonly Mock<ILogger<WeatherController>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockMemoryCache;
    private readonly Mock<IDBWrapper> _mockUnitOfWork;

    public WeatherControllerTests()
    {
        var options = new DbContextOptionsBuilder<WeatherDbContext>()
            .UseInMemoryDatabase("TestDatabase")
            .Options;

        _mockContext = new WeatherDbContext(options);
        _mockLogger = new Mock<ILogger<WeatherController>>();
        var cacheEntryMock = new Mock<ICacheEntry>();
        cacheEntryMock.SetupProperty(f => f.Value);
        _mockMemoryCache = new Mock<IMemoryCache>();
        _mockMemoryCache
            .Setup(m => m.CreateEntry(It.IsAny<object>())) // Mocking CreateEntry call
            .Returns(cacheEntryMock.Object);
        _mockUnitOfWork = new Mock<IDBWrapper>();
        _controller = new WeatherController(_mockContext, _mockLogger.Object, _mockMemoryCache.Object, _mockUnitOfWork.Object);
    }

    private static IFormFile CreateMockFormFile(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var mockFormFile = new Mock<IFormFile>();
        var fileStream = fileInfo.OpenRead();
        var ms = new MemoryStream();
        fileStream.CopyTo(ms);
        ms.Position = 0;

        mockFormFile.Setup(_ => _.OpenReadStream()).Returns(ms);
        mockFormFile.Setup(_ => _.FileName).Returns(fileInfo.Name);
        mockFormFile.Setup(_ => _.Length).Returns(ms.Length);

        return mockFormFile.Object;
    }

    [Fact]
    public void UploadWeatherArchives_WithValidFiles_ProcessesFilesSuccessfully()
    {
        var filePath1 = Path.Combine(Directory.GetCurrentDirectory(), "../../../testFiles", "moskva_2010.xlsx");
        var filePath2 = Path.Combine(Directory.GetCurrentDirectory(), "../../../testFiles", "moskva_2011.xlsx");

        var mockFile1 = CreateMockFormFile(filePath1);
        var mockFile2 = CreateMockFormFile(filePath2);

        var mockFiles = new List<IFormFile> {mockFile1, mockFile2};


        // Act
        var result = _controller.UploadWeatherArchives(mockFiles);
        _mockUnitOfWork.Verify(u => u.BeginTransaction(), Times.Once);
        _mockUnitOfWork.Verify(u => u.Commit(), Times.Once);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewWeatherArchives", redirectToActionResult.ActionName);
        Assert.True(_mockContext.WeatherData.Any());
    }

    [Fact]
    public void ViewWeatherArchives_WithoutParameters_ReturnsViewWithDefaultData()
    {
        // Act
        var result = _controller.ViewWeatherArchives(null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsAssignableFrom<IEnumerable<WeatherData>>(viewResult.Model);
    }

    [Fact]
    public void ViewWeatherArchives_WithYearFilter_ReturnsFilteredData()
    {
        // Arrange
        var testYear = 2010; 

        // Act
        var result = _controller.ViewWeatherArchives(testYear, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IPagedList<WeatherData>>(viewResult.Model);
        Assert.All(model, item => Assert.Equal(testYear, item.Date.Year));
    }

    [Fact]
    public void ViewWeatherArchives_WithMonthFilter_ReturnsFilteredData()
    {
        var testMonth = 5; 

        // Act
        var result = _controller.ViewWeatherArchives(null, testMonth);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IPagedList<WeatherData>>(viewResult.Model);
        Assert.All(model, item => Assert.Equal(testMonth, item.Date.Month));
    }

    [Fact]
    public void UploadWeatherArchives_WithInvalidFile_HandlesErrorGracefully()
    {
        // Arrange
        var invalidFilePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../testFiles", "tcx.txt");
        var mockFile = CreateMockFormFile(invalidFilePath);

        var mockFiles = new List<IFormFile> {mockFile};

        // Act & Assert
        var exception = Record.Exception(() => _controller.UploadWeatherArchives(mockFiles));
        Assert.Null(exception); // Verify that no exception is thrown
    }

    [Fact]
    public void UploadFileAndViewResults_ReturnsViewWithTable()
    {
        // Arrange
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "../../../testFiles", "moskva_2010.xlsx");
        var mockFile = CreateMockFormFile(filePath);
        var mockFiles = new List<IFormFile> {mockFile};

        // Act
        var result = _controller.UploadWeatherArchives(mockFiles);

        // Assert - Check for redirection
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("ViewWeatherArchives", redirectToActionResult.ActionName);

        // Follow the redirection to get the ViewResult
        var viewResult = _controller.ViewWeatherArchives(null, null) as ViewResult;
        Assert.NotNull(viewResult);

        // Verify the view contains a table with data
        var model = Assert.IsAssignableFrom<IPagedList<WeatherData>>(viewResult.Model);
        Assert.NotNull(model);
        Assert.NotEmpty(model);
        Assert.True(_mockContext.WeatherData.Any());
    }
}