using GMap.NET;
using GMap.NET.WindowsForms;
using MeteoMapGeography.UI.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MeteoMapGeography.UI.Forms;
public partial class MainForm : Form
{
    private GMapControl gmap;
    private TrackBar zoomTrackBar;
    private Label loadingLabel;
    private ComboBox countryComboBox;
    private GMapOverlay polygonsOverlay;
    private GeoJsonData geoData;

    public MainForm()
    {
        InitializeComponent();
        CreateControls();
        ToggleControls(false);
        Task.Run(() => LoadPolygonsFromDriveAsync());
        AssingEventHandlers();
    }

    private void AssingEventHandlers()
    {
        gmap.MouseDown += Gmap_MouseDown;
        zoomTrackBar.ValueChanged += ZoomTrackBar_ValueChanged;
        gmap.OnMapZoomChanged += Gmap_OnMapZoomChanged;
    }

    private void CreateControls()
    {
        gmap = new GMapControl
        {
            Dock = DockStyle.Fill,
            MapProvider = GMap.NET.MapProviders.GoogleMapProvider.Instance,
            Position = new PointLatLng(42.5, 1.5),
            MinZoom = 0,
            MaxZoom = 18,
            Zoom = 5
        };
        Controls.Add(gmap);

        zoomTrackBar = new TrackBar
        {
            Orientation = Orientation.Horizontal,
            Minimum = gmap.MinZoom,
            Maximum = gmap.MaxZoom,
            Value = (int)gmap.Zoom,
            Dock = DockStyle.Bottom
        };
        Controls.Add(zoomTrackBar);

        countryComboBox = new ComboBox
        {
            Dock = DockStyle.Top,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        countryComboBox.SelectedIndexChanged += CountryComboBox_SelectedIndexChanged;
        Controls.Add(countryComboBox);

        loadingLabel = new Label
        {
            Text = "Wait, loading data ...",
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Arial", 14, FontStyle.Bold),
            Visible = false
        };
        Controls.Add(loadingLabel);

        polygonsOverlay = new GMapOverlay("polygons");
        gmap.Overlays.Add(polygonsOverlay);
    }

    private void ToggleControls(bool enabled)
    {
        gmap.Enabled = enabled;
        zoomTrackBar.Enabled = enabled;
        countryComboBox.Enabled = enabled;
        loadingLabel.Visible = !enabled;
    }

    private async Task LoadPolygonsFromDriveAsync()
    {
        string googleDriveId = "16s24hYHfYQhKMNcP1hpgQmg13Yb8j0hV";
        string downloadUrl = $"https://drive.google.com/uc?export=download&id={googleDriveId}";

        using (HttpClient client = new HttpClient())
        {
            try
            {
                var jsonData = await client.GetStringAsync(downloadUrl);

                geoData = JsonConvert.DeserializeObject<GeoJsonData>(jsonData);

                Invoke(new Action(() =>
                {
                    PopulateCountryComboBox(geoData);
                    ToggleControls(true);
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error downloading or loading data: {ex.Message}");
                Invoke(new Action(() => ToggleControls(true)));
            }
        }
    }

    private void LoadPolygonsByCountry(string selectedCountry)
    {
        polygonsOverlay.Polygons.Clear();

        foreach (var feature in geoData.Features)
        {
            if (feature.Properties.Country == selectedCountry)
            {
                var geometry = feature.Geometry;
                List<PointLatLng> polygonPoints = GeneratePolygonPoints(geometry);

                var polygon = new GMapPolygon(polygonPoints, feature.Properties.Name)
                {
                    Fill = new SolidBrush(Color.FromArgb(50, Color.Red)),
                    Stroke = new Pen(Color.Red, 1),
                    Tag = feature.Properties
                };

                polygonsOverlay.Polygons.Add(polygon);
            }
        }
    }

    private static List<PointLatLng> GeneratePolygonPoints(Geometry geometry)
    {
        List<PointLatLng> polygonPoints = new List<PointLatLng>();

        if (geometry.Coordinates is JArray coordinatesArray)
        {
            polygonPoints.AddRange(CreatePoints(coordinatesArray));
        }

        return polygonPoints;
    }

    private static List<PointLatLng> CreatePoints(JArray coordinatesArray)
    {
        List<PointLatLng> polygonPoints = new List<PointLatLng>();
        if (coordinatesArray[0] is JArray && coordinatesArray[0][0] is JArray && coordinatesArray[0][0][0] is JArray)
        {
            foreach (var ring in coordinatesArray)
            {
                foreach (var coordArray in ring)
                {
                    polygonPoints.AddRange(CreatePoints(coordArray));

                }
            }
        }
        else if (coordinatesArray[0] is JArray && coordinatesArray[0][0] is JArray)
        {
            polygonPoints.AddRange(CreatePoints(coordinatesArray[0]));
        }
        return polygonPoints;
    }

    private static IEnumerable<PointLatLng> CreatePoints(JToken coordArray)
    {
        var polygonPoints = new List<PointLatLng>();
        foreach (var coord in coordArray)
        {
            polygonPoints.Add(CreatePoint(coord));
        }
        return polygonPoints;
    }

    private static PointLatLng CreatePoint(JToken coord)
    {
        double lat = coord[1].ToObject<double>();
        double lng = coord[0].ToObject<double>();
        return CreatePoint(lat, lng);
    }

    private static PointLatLng CreatePoint(double latitude, double longitude)
    {
        return new PointLatLng(latitude, longitude);
    }

    private void PopulateCountryComboBox(GeoJsonData geoData)
    {
        var uniqueCountries = new HashSet<string>();

        foreach (var feature in geoData.Features)
        {
            uniqueCountries.Add(feature.Properties.Country);
        }

        countryComboBox.Items.Clear();
        countryComboBox.Items.AddRange(uniqueCountries.OrderBy(item => item).ToArray());
        countryComboBox.SelectedIndex = -1;
    }

    private void CountryComboBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        string selectedCountry = countryComboBox.SelectedItem.ToString();
        LoadPolygonsByCountry(selectedCountry);
    }

    private void ZoomTrackBar_ValueChanged(object sender, EventArgs e)
    {
        gmap.Zoom = zoomTrackBar.Value;
    }

    private void Gmap_OnMapZoomChanged()
    {
        zoomTrackBar.Value = (int)gmap.Zoom;
    }

    private void Gmap_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var point = gmap.FromLocalToLatLng(e.X, e.Y);
            foreach (var polygon in polygonsOverlay.Polygons)
            {
                if (polygon.IsInside(point))
                {
                    if (polygon.Tag is Properties properties)
                    {
                        string message = $"Code: {properties.Code}\nCountry: {properties.Country}\nName: {properties.Name}";
                        MessageBox.Show(message, "Poligon Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    return;
                }
            }
        }
    }
}
//}
//public partial class MainForm : Form
//{
//    private readonly IControlService _controlService;
//    private readonly IGeoCodesService _geoCodesService;

//    private GMapControl _gmap;
//    private TrackBar _zoomTrackBar;
//    private Label _loadingLabel;
//    private ComboBox _countryComboBox;
//    private GMapOverlay _polygonsOverlay;

//    public MainForm(IControlService controlService, IGeoCodesService geoCodesService)
//    {
//        _controlService = controlService;
//        _geoCodesService = geoCodesService;

//        InitializeComponent();
//        InitializeControls();
//        ToggleControls(false);
//        Task.Run(() => LoadPolygonsAsync());
//        AssignEventHandlers();
//    }

//    private void InitializeControls()
//    {
//        _gmap = _controlService.CreateGMapControl(new PointLatLng(42.5, 1.5));
//        Controls.Add(_gmap);

//        _zoomTrackBar = _controlService.CreateZoomTrackBar(_gmap);
//        Controls.Add(_zoomTrackBar);

//        _countryComboBox = new ComboBox
//        {
//            Dock = DockStyle.Top,
//            DropDownStyle = ComboBoxStyle.DropDownList
//        };
//        _countryComboBox.SelectedIndexChanged += CountryComboBox_SelectedIndexChanged;
//        Controls.Add(_countryComboBox);

//        _loadingLabel = _controlService.CreateLoadingLabel();
//        Controls.Add(_loadingLabel);

//        _polygonsOverlay = new GMapOverlay("polygons");
//        _gmap.Overlays.Add(_polygonsOverlay);
//    }

//    private void AssignEventHandlers()
//    {
//        _gmap.MouseDown += GMap_MouseDown;
//        _zoomTrackBar.ValueChanged += (s, e) => _gmap.Zoom = _zoomTrackBar.Value;
//        _gmap.OnMapZoomChanged += () => _zoomTrackBar.Value = (int)_gmap.Zoom;
//    }

//    private async Task LoadPolygonsAsync()
//    {
//        try
//        {
//            ToggleControls(false);
//            await _geoCodesService.DownloadGeoCodesAsync();
//            PopulateCountryComboBox();
//            ToggleControls(true);
//        }
//        catch (Exception ex)
//        {
//            MessageBox.Show($"Error loading GeoJSON: {ex.Message}");
//            ToggleControls(true);
//        }
//    }

//    private void PopulateCountryComboBox()
//    {
//        var countries = _geoCodesService.GetCountryCodes().ToArray();

//        _countryComboBox.Items.Clear();
//        _countryComboBox.Items.AddRange(countries);
//        _countryComboBox.SelectedIndex = -1;
//    }

//    private void CountryComboBox_SelectedIndexChanged(object sender, EventArgs e)
//    {
//        string selectedCountry = _countryComboBox.SelectedItem?.ToString();
//        if (!string.IsNullOrEmpty(selectedCountry))
//        {
//            _geoCodesService.LoadPolygonsByCountry(selectedCountry, _polygonsOverlay);
//        }
//    }

//    private void ToggleControls(bool enabled)
//    {
//        _gmap.Enabled = enabled;
//        _zoomTrackBar.Enabled = enabled;
//        _countryComboBox.Enabled = enabled;
//        _loadingLabel.Visible = !enabled;
//    }

//    private void GMap_MouseDown(object sender, MouseEventArgs e)
//    {
//        if (e.Button != MouseButtons.Left) return;

//        var point = _gmap.FromLocalToLatLng(e.X, e.Y);
//        var polygon = _polygonsOverlay.Polygons.FirstOrDefault(p => p.IsInside(point));

//        if (polygon?.Tag is Properties properties)
//        {
//            MessageBox.Show($"Code: {properties.Code}\nCountry: {properties.Country}\nName: {properties.Name}",
//                "Polygon Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
//        }
//    }
//}
