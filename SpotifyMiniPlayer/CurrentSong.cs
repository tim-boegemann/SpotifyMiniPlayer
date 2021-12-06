public class CurrentSong
{
    public long timestamp { get; set; }
    public object context { get; set; }
    public int progress_ms { get; set; }
    public Item item { get; set; }
    public string currently_playing_type { get; set; }
    public Actions actions { get; set; }
    public bool is_playing { get; set; }
}