class_name CatArt
## 繪圖層。
##
## **所有視覺都集中在這裡，遊戲腳本裡不再有任何繪圖程式碼。**
## 每個函式收一個 CanvasItem，在它的 _draw() 期間被呼叫。
##
## 有圖就用圖（res://art/ 裡去背後的 PNG），沒有圖就退回這裡的程式繪圖。
## 圖的清單與生圖提示詞在 PROMPTS.md，顏色對照 STYLE.md。

# ── 色盤 ────────────────────────────────────────────────────
const INK            := Color(0.231, 0.165, 0.145)   # #3B2A25 輪廓

# 第 1 關：後院的木頭走廊
const PLANK          := Color(0.804, 0.686, 0.525)   # #CDAF86 木板
const PLANK_ALT      := Color(0.827, 0.714, 0.557)   # 相鄰木板稍微亮一點
const PLANK_LINE     := Color(0.737, 0.612, 0.447)   # #BC9C72 木板線
const WALL_FILL      := Color(0.965, 0.933, 0.875)   # 外框：米白色欄杆
const ISLAND_FILL    := Color(0.902, 0.827, 0.710)   # 內外道分隔島

const FUR            := Color(0.961, 0.925, 0.867)   # 可樂娜的白爪
const FUR_SHADE      := Color(0.855, 0.804, 0.729)
const BEAN           := Color(0.937, 0.573, 0.639)   # 肉球、鼻子
const CLAW           := Color(1.0, 0.988, 0.949)
const COLONA_BROWN   := Color(0.478, 0.361, 0.282)   # #7A5C48

const YARN           := Color(0.898, 0.361, 0.302)   # 毛線球
const YARN_DARK      := Color(0.729, 0.243, 0.204)
const YARN_LIGHT     := Color(0.976, 0.588, 0.533)

const EAR_FUR        := Color(0.855, 0.769, 0.686)   # slingshot 貓耳
const EAR_INNER      := Color(0.898, 0.549, 0.612)
const EAR_RIM        := Color(1.0, 0.831, 0.463)
const EAR_DEAD       := Color(0.376, 0.302, 0.298)

const SNOW_FUR       := Color(0.957, 0.965, 0.973)   # 雪兒：冷白 #F4F6F8
const SNOW_SHADE     := Color(0.788, 0.827, 0.863)   # #C9D3DC
const SNOW_EYE       := Color(1.0, 0.706, 0.239)     # #FFB43D
const SNOW_SPOT      := Color(0.353, 0.353, 0.369)   # 額頭灰斑 #5A5A5E
const EAR_PINK       := Color(0.847, 0.714, 0.643)   # 耳內 #D8B6A4

const JINBAO_FUR     := Color(0.184, 0.169, 0.188)   # 進寶：炭黑 #2F2B30
const JINBAO_LIGHT   := Color(0.290, 0.271, 0.314)   # 亮面 #4A4550
const JINBAO_EYE     := Color(0.949, 0.698, 0.2)     # #F2B233
const BOX            := Color(0.784, 0.608, 0.416)   # 紙箱頭盔 #C89B6A
const BOX_SHADE      := Color(0.659, 0.490, 0.310)   # #A87D4F

const RACCOON        := Color(0.604, 0.604, 0.639)   # 大嘴浣熊 #9A9AA3
const RACCOON_SHADE  := Color(0.482, 0.482, 0.522)   # #7B7B85
const RACCOON_MASK   := Color(0.243, 0.227, 0.251)   # 面罩 #3E3A40
const RACCOON_CREAM  := Color(0.902, 0.882, 0.847)   # 嘴邊 #E6E1D8
const PATCH          := Color(0.122, 0.110, 0.129)   # 眼罩 #1F1C21
const LID            := Color(0.788, 0.820, 0.847)   # 鍋蓋盾牌 #C9D1D8
const LID_SHADE      := Color(0.639, 0.678, 0.714)

const SQUIRREL       := Color(0.851, 0.518, 0.243)   # 松鼠小弟 #D9843E
const SQUIRREL_SHADE := Color(0.722, 0.404, 0.169)   # #B8672B
const SQUIRREL_BELLY := Color(0.961, 0.890, 0.769)   # #F5E3C4
const ACORN          := Color(0.659, 0.420, 0.235)   # 橡實 #A86B3C
const ACORN_CAP      := Color(0.431, 0.290, 0.180)   # #6E4A2E

const POT            := Color(0.851, 0.467, 0.306)   # 花盆：陶土色
const POT_DARK       := Color(0.690, 0.349, 0.216)
const SOIL           := Color(0.412, 0.290, 0.212)
const LEAF           := Color(0.486, 0.765, 0.420)
const LEAF_DARK      := Color(0.353, 0.620, 0.310)

const HURT           := Color(1.0, 0.635, 0.573)     # 受傷閃爍
const BELL           := Color(1.0, 0.831, 0.361)     # 血量環、蓄力條 #FFCB3D
const DANGER         := Color(1.0, 0.212, 0.239)     # 攻擊預警 #FF363D
const FISH           := Color(0.643, 0.855, 0.949)   # 小魚乾

# 第 2 關：公園草地
const GRASS          := Color(0.745, 0.855, 0.620)   # 草地
const GRASS_STRIPE   := Color(0.702, 0.827, 0.573)   # 割草機壓出來的條紋
const MOLE           := Color(0.435, 0.357, 0.337)   # 小鼴鼠阿吉
const MOLE_LIGHT     := Color(0.886, 0.733, 0.659)   # 口鼻、爪子
const GOGGLE         := Color(0.667, 0.878, 0.941)   # 護目鏡
const DIRT           := Color(0.545, 0.388, 0.259)   # 洞口的土、泥巴
const DIRT_DARK      := Color(0.259, 0.180, 0.129)   # 洞裡

const PUG            := Color(0.894, 0.788, 0.635)   # 巴哥老大：淺米色毛 #E4C9A2
const PUG_SHADE      := Color(0.780, 0.655, 0.490)   # 額頭皺紋
const PUG_MASK       := Color(0.290, 0.235, 0.224)   # 黑臉、耳朵 #4A3C39
const COLLAR         := Color(0.176, 0.157, 0.169)   # 巴哥的黑色鉚釘皮項圈
const STUD           := Color(0.839, 0.851, 0.871)   # 銀色尖刺
const GOLD           := Color(0.953, 0.776, 0.286)   # 金鍊、金牌
const GOLD_DARK      := Color(0.788, 0.596, 0.161)
const TOY_RED        := Color(0.949, 0.361, 0.329)   # 玩具球、皮球
const TOY_YELLOW     := Color(0.992, 0.800, 0.302)
const TOY_BLUE       := Color(0.400, 0.702, 0.949)
const BONE           := Color(0.980, 0.949, 0.862)   # 骨頭
const HEDGEHOG       := Color(0.545, 0.420, 0.329)   # 小刺蝟的刺 #8B6B54
const HEDGEHOG_DARK  := Color(0.420, 0.314, 0.243)
const HEDGEHOG_FACE  := Color(0.961, 0.882, 0.761)
const HELMET         := Color(1.0, 0.804, 0.204)     # 安全帽


# ── 背景 = 後院的木頭走廊 ───────────────────────────────────

const PLAYFIELD_TEX := "res://art/playfield.png"

static var _playfield_loaded := false
static var _playfield_tex: Texture2D


## 檯面底圖。有 art/playfield.png（tools/key_green.py 裁成 9:16）就整張鋪滿，
## 沒有就畫縱向的木板。牆不在這裡，由 Table 照碰撞線畫。
static func porch(c: CanvasItem, w: float, h: float) -> void:
	if not _playfield_loaded:
		_playfield_loaded = true
		if ResourceLoader.exists(PLAYFIELD_TEX):
			_playfield_tex = load(PLAYFIELD_TEX)
	# 畫面震動時鏡頭會偏移，四周多鋪一圈，不然邊緣會露出底色
	var m := 48.0
	c.draw_rect(Rect2(-m, -m, w + m * 2.0, h + m * 2.0), PLANK)
	if _playfield_tex != null:
		c.draw_texture_rect(_playfield_tex, Rect2(0, 0, w, h), false)
		return

	var board := 72.0
	var i := 0
	var x := -m
	while x < w + m:
		if i % 2 == 1:
			c.draw_rect(Rect2(x, -m, board, h + m * 2.0), PLANK_ALT)
		c.draw_line(Vector2(x, -m), Vector2(x, h + m), PLANK_LINE, 2.0)
		# 木板的接縫，每一條錯開高度
		var y := fmod(i * 257.0, 420.0) - m
		while y < h + m:
			c.draw_line(Vector2(x, y), Vector2(x + board, y), PLANK_LINE, 2.0)
			y += 420.0
		x += board
		i += 1


# ── 背景 = 公園草地（第 2 關）───────────────────────────────

## 有 art/playfield_park.png 就整張鋪滿，沒有就畫割草條紋。
static func park(c: CanvasItem, w: float, h: float) -> void:
	var m := 48.0
	c.draw_rect(Rect2(-m, -m, w + m * 2.0, h + m * 2.0), GRASS)
	var tex := _art_tex("playfield_park")
	if tex != null:
		c.draw_texture_rect(tex, Rect2(0, 0, w, h), false)
		return
	var band := 80.0
	var y := -m
	var i := 0
	while y < h + m:
		if i % 2 == 1:
			c.draw_rect(Rect2(-m, y, w + m * 2.0, band), GRASS_STRIPE)
		y += band
		i += 1
	# 幾撮小草
	var tuft := GRASS_STRIPE.darkened(0.25)
	for k in 26:
		var p := Vector2(fmod(k * 263.0, w), fmod(k * 457.0 + 60.0, h))
		c.draw_line(p, p + Vector2(-4, -9), tuft, 2.0)
		c.draw_line(p, p + Vector2(4, -9), tuft, 2.0)


# ── 背景 = 公園的溜滑梯區（第 3 關）─────────────────────────

## 有 art/playfield_slide.png 就整張鋪滿，沒有就先用第二關的公園草地
static func playground(c: CanvasItem, w: float, h: float) -> void:
	var m := 48.0
	c.draw_rect(Rect2(-m, -m, w + m * 2.0, h + m * 2.0), GRASS)
	var tex := _art_tex("playfield_slide")
	if tex == null:
		park(c, w, h)
		return
	c.draw_texture_rect(tex, Rect2(0, 0, w, h), false)


# ── 球 = 毛線球 ─────────────────────────────────────────────

## 外道救球彈簧（三關共用）：外道裡的一個貓掌燈。
## ready = 充好了（綠圈、肉球亮）；charge = 充電進度 0～1（紅圈一圈一圈長回來）；flash = 剛把球踢回去
static func kickback(c: CanvasItem, at: Vector2, ready: bool, charge: float, flash: float) -> void:
	var col := Color(0.55, 1.0, 0.65) if ready else Color(1.0, 0.55, 0.45)
	var a0 := -PI * 0.5
	var sweep := TAU if ready else TAU * clampf(charge, 0.0, 1.0)
	c.draw_circle(at, 15.0 + 6.0 * flash, Color(col, 0.16 + 0.35 * flash))
	c.draw_arc(at, 14.0, 0.0, TAU, 28, Color(INK, 0.35), 3.0, true)
	if sweep > 0.01:
		c.draw_arc(at, 14.0, a0, a0 + sweep, 28, INK, 6.0, true)
		c.draw_arc(at, 14.0, a0, a0 + sweep, 28, col, 3.5, true)
	# 中間的肉球：充好了才亮
	var bean := BEAN if ready else Color(BEAN, 0.3)
	c.draw_circle(at + Vector2(0.0, 2.5), 5.0, bean)
	for k in 3:
		c.draw_circle(at + Vector2((k - 1) * 4.5, -4.5 + absf(k - 1) * 1.5), 2.0, bean)


## 雙球時的第二顆球：進寶縮成一顆黑色的球（暗黑騎士飛天撲擊）
static func jinbao_ball(c: CanvasItem, r: float) -> void:
	if _disc_sprite(c, "ball_jinbao", r):
		return
	for s in [-1.0, 1.0]:
		c.draw_colored_polygon(PackedVector2Array([
			Vector2(s * r * 0.15, -r * 0.8), Vector2(s * r * 1.05, -r * 1.15), Vector2(s * r * 0.9, -r * 0.1)]), INK)
	c.draw_circle(Vector2.ZERO, r + 2.5, INK)
	c.draw_circle(Vector2.ZERO, r, JINBAO_FUR)
	for s in [-1.0, 1.0]:
		c.draw_colored_polygon(PackedVector2Array([
			Vector2(s * r * 0.35, -r * 0.78), Vector2(s * r * 0.88, -r * 0.98), Vector2(s * r * 0.8, -r * 0.4)]),
			JINBAO_LIGHT)
		c.draw_circle(Vector2(s * r * 0.36, -r * 0.02), r * 0.24, JINBAO_EYE)
		c.draw_circle(Vector2(s * r * 0.36, -r * 0.02), r * 0.11, INK)
	c.draw_circle(Vector2(-r * 0.42, -r * 0.48), r * 0.14, Color(1, 1, 1, 0.28))


static func yarn_ball(c: CanvasItem, r: float, at := Vector2.ZERO) -> void:
	# 圖片版自己會設 transform，所以位置要用 at 傳進來，不能靠外面先 draw_set_transform
	if _disc_sprite(c, "yarn", r, at):
		return
	c.draw_set_transform(at, 0.0, Vector2.ONE)
	c.draw_circle(Vector2.ZERO, r + 2.5, INK)
	c.draw_circle(Vector2.ZERO, r, YARN)
	c.draw_circle(Vector2(-r * 0.28, -r * 0.28), r * 0.62, YARN_LIGHT)
	# 纏繞的線：球自轉時看得出來
	for i in 3:
		var a := PI * i / 3.0
		c.draw_set_transform(Vector2.ZERO, a, Vector2(1.0, 0.42))
		c.draw_arc(Vector2.ZERO, r * 0.92, 0.0, TAU, 28, YARN_DARK, 2.0, true)
		c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	c.draw_arc(Vector2.ZERO, r - 1.0, 0.0, TAU, 32, YARN_DARK, 2.0, true)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


# ── 翼板 = 可樂娜的爪 ──────────────────────────────────────

## full_length：沒被打短時的長度（決定圖的縮放）。mirror：-1 表示畫成左爪的鏡像。
static func paw(c: CanvasItem, rot: float, length: float, thick: float,
		claws_out: bool, tint: Color, full_length: float = 0.0, mirror: float = 1.0) -> void:
	if _paw_sprite(c, rot, length, full_length if full_length > 0.0 else length,
			thick, claws_out, tint, mirror):
		return
	c.draw_set_transform(Vector2.ZERO, rot, Vector2.ONE)

	var fur: Color = FUR * tint
	var shade: Color = FUR_SHADE * tint
	var pad_x := length * 0.80
	var tip_r := thick * 1.42

	# 前臂
	c.draw_line(Vector2(0, 0), Vector2(pad_x, 0), shade, thick * 2.1)
	c.draw_line(Vector2(0, -thick * 0.30), Vector2(pad_x, -thick * 0.30), fur, thick * 1.25)
	c.draw_circle(Vector2.ZERO, thick * 1.02, shade)

	# 爪子（按下才伸出來）
	if claws_out:
		for i in 3:
			var a: float = deg_to_rad(-34.0 + i * 34.0)
			var base := Vector2(pad_x, 0) + Vector2(tip_r * 0.95, 0).rotated(a)
			var tip := Vector2(pad_x, 0) + Vector2(tip_r * 2.05, 0).rotated(a)
			c.draw_line(base, tip, CLAW * tint, 4.0)

	# 掌
	c.draw_circle(Vector2(pad_x, 0), tip_r, fur)
	# 三顆趾球
	for i in 3:
		var a2: float = deg_to_rad(-40.0 + i * 40.0)
		c.draw_circle(Vector2(pad_x, 0) + Vector2(tip_r * 0.62, 0).rotated(a2),
			thick * 0.40, BEAN * tint)
	# 大肉球
	# draw_set_transform 的 position 是「未旋轉」的區域座標，要自己先轉
	c.draw_set_transform(Vector2(pad_x - thick * 0.45, 0).rotated(rot), rot, Vector2(1.0, 0.78))
	c.draw_circle(Vector2.ZERO, thick * 0.66, BEAN * tint)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


const PAW_TEX := "res://art/paw.png"
const PAW_CLAWS_TEX := "res://art/paw_claws.png"
const PAW_META := "res://art/paw.json"

static var _paw_loaded := false
static var _paw_tex: Texture2D
static var _paw_claws_tex: Texture2D
static var _paw_meta: Dictionary = {}


static func _load_paw() -> void:
	_paw_loaded = true
	if not ResourceLoader.exists(PAW_TEX) or not FileAccess.file_exists(PAW_META):
		return
	_paw_meta = _read_json(PAW_META)
	if _paw_meta.is_empty():
		return
	_paw_tex = load(PAW_TEX)
	if ResourceLoader.exists(PAW_CLAWS_TEX):
		_paw_claws_tex = load(PAW_CLAWS_TEX)


## 用圖畫貓爪。數字來自 tools/key_green.py 量出來的 art/paw.json：
##   pivots     每張圖的旋轉支點（前臂左端的中心，像素座標）
##   length_px  貓爪從左端到指尖的長度 → 縮放成碰撞長度 full_length
##   angle_deg  圖裡的貓爪沒有完全水平時的傾斜角，畫的時候轉回來
## 被橡實砸到時不整隻縮小（會變得比碰撞體細），而是從支點那端裁掉，
## 看起來像手臂縮回去，粗細仍然和碰撞一致。
static func _paw_sprite(c: CanvasItem, rot: float, length: float, full_length: float,
		thick: float, claws_out: bool, tint: Color, mirror: float) -> bool:
	if not _paw_loaded:
		_load_paw()
	if _paw_tex == null:
		return false

	var use_claws: bool = claws_out and _paw_claws_tex != null
	var tex: Texture2D = _paw_claws_tex if use_claws else _paw_tex
	var pivots: Dictionary = _paw_meta.get("pivots", {})
	var pv: Array = pivots.get("paw_claws" if use_claws else "paw", pivots.get("paw", [0.0, 0.0]))
	var pivot := Vector2(float(pv[0]), float(pv[1]))
	var art_len := float(_paw_meta.get("length_px", 1.0))
	var art_angle := deg_to_rad(float(_paw_meta.get("angle_deg", 0.0)))
	var k := full_length / art_len

	# 被打短：沿主軸把起點往指尖方向移，起點左邊的部分不畫
	var cut := maxf(0.0, art_len - length / k)
	var start := pivot + Vector2(cos(art_angle), sin(art_angle)) * cut
	var tex_size := Vector2(tex.get_width(), tex.get_height())
	var src := Rect2(Vector2.ZERO, tex_size)
	if cut > 0.0:
		src = Rect2(Vector2(start.x, 0.0), Vector2(tex_size.x - start.x, tex_size.y))

	# 貼圖座標 → 翼板座標：先把圖的傾斜轉正、縮放（右爪沿主軸翻面），再轉到翼板角度
	var xf := Transform2D(rot, Vector2.ZERO) \
		* Transform2D(Vector2(k, 0.0), Vector2(0.0, k * mirror), Vector2.ZERO) \
		* Transform2D(-art_angle, Vector2.ZERO)
	c.draw_set_transform_matrix(xf)
	c.draw_texture_rect_region(tex, Rect2(src.position - start, src.size), src, tint)
	c.draw_set_transform_matrix(Transform2D.IDENTITY)

	if cut > 0.0:
		# 裁切處蓋一個手臂斷面，不然會看到一刀切平的邊
		c.draw_circle(Vector2.ZERO, thick, INK)
		c.draw_circle(Vector2.ZERO, thick - 2.5, COLONA_BROWN * tint)
	return true


# ── 上面三顆 bumper = 花盆（會彈球、不開砲）────────────────────

static func flower_pot(c: CanvasItem, r: float, heat: float) -> void:
	if _disc_sprite(c, "flowerpot", r):
		_hit_glow(c, r, heat)
		return
	# 正上方俯視：盆緣、泥土、中間一株小植物
	c.draw_circle(Vector2.ZERO, r + 3.0, INK)
	c.draw_circle(Vector2.ZERO, r, POT.lerp(Color.WHITE, heat * 0.4))
	c.draw_arc(Vector2(r * 0.06, r * 0.06), r * 0.84, 0.2, PI * 0.9, 16, POT_DARK, 3.0, true)
	c.draw_circle(Vector2.ZERO, r * 0.72, INK)
	c.draw_circle(Vector2.ZERO, r * 0.68, SOIL)
	for i in 5:
		var a := TAU * i / 5.0 - PI * 0.5
		var at := Vector2(r * 0.30, 0).rotated(a)
		_ellipse(c, at, r * 0.26, r * 0.17, INK, a)
		_ellipse(c, at, r * 0.22, r * 0.13, LEAF if i % 2 == 0 else LEAF_DARK, a)
	c.draw_circle(Vector2.ZERO, r * 0.10, LEAF_DARK)


# ── 下面兩座砲台 = 雪兒（左）與進寶（右）──────────────────────

## who = "snow" / "jinbao"
static func buddy_face(c: CanvasItem, r: float, heat: float, who: String) -> void:
	if _disc_sprite(c, "turret_" + who, r):
		_hit_glow(c, r, heat)
		return
	# jinbao_bare：第二關一開始頭盔被偷走的進寶
	var jinbao := who.begins_with("jinbao")
	var fur := (JINBAO_FUR if jinbao else SNOW_FUR).lerp(Color.WHITE, heat * 0.35)
	_cat_ears(c, r, fur, JINBAO_LIGHT if jinbao else EAR_PINK)
	c.draw_circle(Vector2.ZERO, r + 3.0, INK)
	if jinbao:
		c.draw_circle(Vector2.ZERO, r, fur)
		c.draw_circle(Vector2(-r * 0.34, -r * 0.30), r * 0.30, JINBAO_LIGHT)
	else:
		c.draw_circle(Vector2.ZERO, r, SNOW_SHADE)
		c.draw_circle(Vector2(-r * 0.08, -r * 0.10), r * 0.86, fur)
		c.draw_circle(Vector2(-r * 0.40, -r * 0.50), r * 0.17, SNOW_SPOT)

	var eye := JINBAO_EYE if jinbao else SNOW_EYE
	for s in [-1.0, 1.0]:
		var at := Vector2(s * r * 0.36, -r * 0.06)
		c.draw_circle(at, r * 0.21, INK)
		c.draw_circle(at, r * 0.17, eye)
		c.draw_circle(at, r * 0.07, INK)
	# 黑貓臉上的線用亮一點的顏色，不然看不到
	var line := JINBAO_LIGHT.lightened(0.35) if jinbao else INK
	c.draw_colored_polygon(PackedVector2Array([
		Vector2(-r * 0.09, r * 0.18), Vector2(r * 0.09, r * 0.18), Vector2(0, r * 0.29)]), BEAN)
	c.draw_arc(Vector2(-r * 0.11, r * 0.32), r * 0.11, 0.0, PI, 10, line, 2.0, true)
	c.draw_arc(Vector2(r * 0.11, r * 0.32), r * 0.11, 0.0, PI, 10, line, 2.0, true)

	if who == "jinbao":
		# 紙箱頭盔：蓋在頭頂，耳尖從兩側露出來
		var box := Rect2(-r * 0.58, -r * 1.04, r * 1.16, r * 0.56)
		c.draw_rect(box.grow(2.5), INK)
		c.draw_rect(box, BOX)
		c.draw_rect(Rect2(box.position.x + box.size.x * 0.64, box.position.y,
			box.size.x * 0.36, box.size.y), BOX_SHADE)
		c.draw_line(Vector2(0, box.position.y), Vector2(0, box.end.y), INK, 2.0)


# ── 第二關的障礙物 = 石頭 ───────────────────────────────────

## 正上方俯視的圓石頭，會彈球、不開砲
static func rock(c: CanvasItem, r: float, heat: float) -> void:
	if _disc_sprite(c, "rock", r):
		_hit_glow(c, r, heat)
		return
	var stone := Color(0.64, 0.64, 0.62).lerp(Color.WHITE, heat * 0.4)
	var pts := PackedVector2Array()
	for i in 12:
		var a := TAU * i / 12.0
		pts.append(Vector2(cos(a), sin(a)) * r * (1.0 + 0.07 * sin(a * 3.0 + 0.6)))
	var outline := PackedVector2Array()
	for p in pts:
		outline.append(p * ((r + 3.0) / r))
	c.draw_colored_polygon(outline, INK)
	c.draw_colored_polygon(pts, Color(0.50, 0.50, 0.49))
	var top := PackedVector2Array()
	for p in pts:
		top.append(p * 0.82 + Vector2(-r * 0.08, -r * 0.08))
	c.draw_colored_polygon(top, stone)
	_ellipse(c, Vector2(-r * 0.35, -r * 0.38), r * 0.22, r * 0.11, Color(1, 1, 1, 0.4), -0.6)
	c.draw_polyline(PackedVector2Array([
		Vector2(r * 0.1, -r * 0.2), Vector2(r * 0.26, r * 0.05), Vector2(r * 0.14, r * 0.3)]), INK, 2.0)
	# 石頭邊的一小撮草
	for s in [-1.0, 0.0, 1.0]:
		c.draw_line(Vector2(-r * 0.55 + s * 5.0, r * 0.66), Vector2(-r * 0.55 + s * 8.0, r * 0.38), LEAF_DARK, 3.0)


# ── Slingshot = 貓耳 ────────────────────────────────────────

static func cat_ear(c: CanvasItem, poly: PackedVector2Array,
		face_a: Vector2, face_b: Vector2, alive: bool, flash: float) -> void:
	var fur := (EAR_FUR if alive else EAR_DEAD).lerp(Color.WHITE, flash * 0.5)
	c.draw_colored_polygon(poly, fur)
	var rim_line := PackedVector2Array(poly)
	rim_line.append(poly[0])
	c.draw_polyline(rim_line, INK, 5.0, true)
	# 耳內粉色：把多邊形往重心縮
	var ctr := Vector2.ZERO
	for p in poly:
		ctr += p
	ctr /= float(poly.size())
	var inner := PackedVector2Array()
	for p in poly:
		inner.append(ctr.lerp(p, 0.58))
	c.draw_colored_polygon(inner, EAR_INNER if alive else EAR_DEAD.lightened(0.1))
	# 彈射邊緣
	var rim := (EAR_RIM if alive else EAR_DEAD.lightened(0.18)).lerp(Color.WHITE, flash)
	c.draw_line(face_a, face_b, rim, 6.0 + 5.0 * flash)


# ── 敵人本體 = 大嘴浣熊 ─────────────────────────────────────

## shielded = 松鼠小弟還在，舉著鍋蓋打不動。鍋蓋掉了才顯示血量環。
static func raccoon(c: CanvasItem, r: float, shielded: bool, flash: float,
		hp: int = 0, max_hp: int = 0) -> void:
	var tint := Color.WHITE.lerp(Color(1.0, 0.55, 0.5), flash)
	# 兩種狀態用同一顆頭（從 A3 設定圖切出來的），鍋蓋由程式畫上去，保證是同一隻浣熊
	if _disc_sprite(c, "raccoon", r, Vector2.ZERO, tint):
		if shielded:
			_pot_lid(c, r)
		else:
			# 鍋蓋掉了：冒一滴汗
			c.draw_circle(Vector2(r * 0.86, -r * 0.62), r * 0.14 + 2.0, INK)
			c.draw_circle(Vector2(r * 0.86, -r * 0.62), r * 0.14, FISH)
			_hp_ring(c, r + 8.0, hp, max_hp, 3.5)
		return

	var fur := RACCOON.lerp(HURT, flash)
	for s in [-1.0, 1.0]:
		var ear := Vector2(s * r * 0.62, -r * 0.70)
		c.draw_circle(ear, r * 0.30 + 3.0, INK)
		c.draw_circle(ear, r * 0.30, fur)
		c.draw_circle(ear, r * 0.16, RACCOON_MASK)
	c.draw_circle(Vector2.ZERO, r + 3.0, INK)
	c.draw_circle(Vector2.ZERO, r, RACCOON_SHADE.lerp(HURT, flash))
	c.draw_circle(Vector2(-r * 0.08, -r * 0.10), r * 0.86, fur)
	_ellipse(c, Vector2(0, r * 0.36), r * 0.54, r * 0.34, RACCOON_CREAM)
	# 浣熊天生的黑面罩
	_ellipse(c, Vector2(0, -r * 0.14), r * 0.80, r * 0.32, RACCOON_MASK)
	# 右眼露出來，左眼戴海盜眼罩
	c.draw_circle(Vector2(r * 0.34, -r * 0.14), r * 0.16, Color.WHITE)
	c.draw_circle(Vector2(r * 0.37, -r * 0.13), r * 0.08, INK)
	c.draw_line(Vector2(-r * 0.92, -r * 0.02), Vector2(r * 0.60, -r * 0.66), PATCH, 3.0)
	c.draw_circle(Vector2(-r * 0.34, -r * 0.14), r * 0.21, PATCH)
	c.draw_circle(Vector2(0, r * 0.12), r * 0.09, INK)

	if shielded:
		_pot_lid(c, r)
	else:
		# 鍋蓋掉了：張大嘴慌張，冒一滴汗
		_ellipse(c, Vector2(0, r * 0.46), r * 0.30, r * 0.22, INK)
		_ellipse(c, Vector2(0, r * 0.54), r * 0.18, r * 0.09, BEAN)
		c.draw_circle(Vector2(r * 0.78, -r * 0.52), r * 0.13 + 2.0, INK)
		c.draw_circle(Vector2(r * 0.78, -r * 0.52), r * 0.13, FISH)
		_hp_ring(c, r + 8.0, hp, max_hp, 3.5)


## 鍋蓋盾牌：擋住嘴和下巴，只露出眼睛；兩隻灰色小手抓著左右兩邊
static func _pot_lid(c: CanvasItem, r: float) -> void:
	var ctr := Vector2(0, r * 0.46)
	_ellipse(c, ctr, r * 0.98 + 3.0, r * 0.52 + 3.0, INK)
	_ellipse(c, ctr, r * 0.98, r * 0.52, LID_SHADE)
	_ellipse(c, ctr + Vector2(-r * 0.06, -r * 0.05), r * 0.86, r * 0.42, LID)
	c.draw_circle(ctr + Vector2(0, -r * 0.10), r * 0.13 + 2.5, INK)
	c.draw_circle(ctr + Vector2(0, -r * 0.10), r * 0.13, LID_SHADE)
	for s in [-1.0, 1.0]:
		var paw := ctr + Vector2(s * r * 0.92, r * 0.12)
		c.draw_circle(paw, r * 0.17 + 2.5, INK)
		c.draw_circle(paw, r * 0.17, RACCOON)


# ── 可拆部位 = 松鼠小弟 ─────────────────────────────────────

## mirror = -1 左右翻面（左邊那隻），一對看起來才對稱。
## wind_up = 預警時把橡實舉起來的程度（0～1）。
static func squirrel(c: CanvasItem, r: float, flash: float, hp: int, max_hp: int,
		mirror: float = 1.0, wind_up: float = 0.0) -> void:
	var tint := Color.WHITE.lerp(Color(1.0, 0.55, 0.5), flash)
	# 預警時整隻往上蹦一下
	var hop := Vector2(0.0, -r * 0.14 * wind_up)
	if _disc_sprite(c, "squirrel", r, hop, tint, mirror):
		_hp_ring(c, r + 9.0, hp, max_hp, 3.0)
		return

	var fur := SQUIRREL.lerp(HURT, flash)
	# 大尾巴：從身體後面往上捲
	var tail := hop + Vector2(mirror * r * 0.66, -r * 0.30)
	_ellipse(c, tail, r * 0.50 + 3.0, r * 0.82 + 3.0, INK)
	_ellipse(c, tail, r * 0.50, r * 0.82, SQUIRREL_SHADE.lerp(HURT, flash))
	for s in [-1.0, 1.0]:
		var ear := hop + Vector2(s * r * 0.52, -r * 0.84)
		c.draw_circle(ear, r * 0.20 + 2.5, INK)
		c.draw_circle(ear, r * 0.20, fur)
	c.draw_circle(hop, r + 3.0, INK)
	c.draw_circle(hop, r, fur)
	_ellipse(c, hop + Vector2(0, r * 0.38), r * 0.56, r * 0.46, SQUIRREL_BELLY)
	# 一隻眼睛＋跟老大同款的眼罩
	c.draw_circle(hop + Vector2(mirror * r * 0.32, -r * 0.20), r * 0.13, INK)
	c.draw_circle(hop + Vector2(mirror * r * 0.36, -r * 0.24), r * 0.05, Color.WHITE)
	c.draw_line(hop + Vector2(-mirror * r * 0.95, -r * 0.02), hop + Vector2(mirror * r * 0.55, -r * 0.64), PATCH, 2.5)
	c.draw_circle(hop + Vector2(-mirror * r * 0.32, -r * 0.20), r * 0.19, PATCH)
	c.draw_circle(hop + Vector2(0, r * 0.02), r * 0.07, INK)
	c.draw_rect(Rect2(hop + Vector2(-r * 0.08, r * 0.10), Vector2(r * 0.16, r * 0.14)), Color.WHITE)
	# 手上的橡實：預警時舉高、變大
	acorn(c, hop + Vector2(0, -r * (1.02 + 0.30 * wind_up)), 0.0, r * 0.26 * (1.0 + 0.3 * wind_up))
	_hp_ring(c, r + 9.0, hp, max_hp, 3.0)


# ── 第 2 關的敵人 = 小鼴鼠阿吉＋地洞 ────────────────────────

## 阿吉從洞口探出上半身。armored = 洞還開著、打不動；dizzy = 被困在最後一個洞裡
static func mole(c: CanvasItem, r: float, armored: bool, dizzy: bool, flash: float,
		hp: int, max_hp: int, wind_up: float = 0.0, box: int = 0) -> void:
	var tint := Color.WHITE.lerp(Color(1.0, 0.55, 0.5), flash)
	# 圖上只有頭跟爪子，會把腳下的洞整個蓋住：先在後面畫一圈比爪子寬的洞口
	if _art_tex("mole") != null and not _disc_sprite(c, "hole", r * 1.3, Vector2(0, r * 0.55)):
		_ellipse(c, Vector2(0, r * 0.72), r * 1.35 + 3.0, r * 0.5 + 3.0, INK)
		_ellipse(c, Vector2(0, r * 0.72), r * 1.35, r * 0.5, DIRT)
		_ellipse(c, Vector2(0, r * 0.66), r * 1.05, r * 0.34, DIRT_DARK)
	if not _disc_sprite(c, "mole_dizzy" if dizzy else "mole", r, Vector2.ZERO, tint, 1.0, "mole"):
		_mole_body(c, r, dizzy, flash)
	# 第二階段：偷來的紙箱（進寶的頭盔）戴在頭上。被打一下就歪掉、裂開
	if box > 0:
		c.draw_set_transform(Vector2(0, -r * 0.95), 0.0 if box >= 2 else 0.35, Vector2.ONE)
		var rect := Rect2(-r * 0.62, -r * 0.42, r * 1.24, r * 0.6)
		c.draw_rect(rect.grow(2.5), INK)
		c.draw_rect(rect, BOX)
		c.draw_rect(Rect2(rect.position.x + rect.size.x * 0.64, rect.position.y,
			rect.size.x * 0.36, rect.size.y), BOX_SHADE)
		c.draw_line(Vector2(0, rect.position.y), Vector2(0, rect.end.y), INK, 2.0)
		if box < 2:
			c.draw_polyline(PackedVector2Array([
				Vector2(-r * 0.45, rect.position.y + 2.0), Vector2(-r * 0.3, -r * 0.2),
				Vector2(-r * 0.4, -r * 0.05), Vector2(-r * 0.2, rect.end.y - 2.0)]), INK, 2.0)
		c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	# 預警：手上的泥巴球舉高
	if wind_up > 0.0:
		mud_ball(c, Vector2(r * 0.85, -r * (0.95 + 0.35 * wind_up)), 0.0, r * 0.26 * (1.0 + 0.3 * wind_up))
	# 暈頭轉向：頭上繞圈的星星
	if dizzy:
		var t := Time.get_ticks_msec() / 400.0
		for i in 3:
			var a := t + TAU * i / 3.0
			var p := Vector2(cos(a) * r * 0.8, -r * 1.1 + sin(a) * r * 0.2)
			c.draw_circle(p, r * 0.12 + 1.5, INK)
			c.draw_circle(p, r * 0.12, BELL)
	if not armored:
		_hp_ring(c, r + 8.0, hp, max_hp, 3.5)


static func _mole_body(c: CanvasItem, r: float, dizzy: bool, flash: float) -> void:
	var fur := MOLE.lerp(HURT, flash)
	# 洞口一圈土
	_ellipse(c, Vector2(0, r * 0.62), r * 1.1 + 3.0, r * 0.42 + 3.0, INK)
	_ellipse(c, Vector2(0, r * 0.62), r * 1.1, r * 0.42, DIRT)
	c.draw_circle(Vector2(0, -r * 0.08), r + 3.0, INK)
	c.draw_circle(Vector2(0, -r * 0.08), r, fur)
	_ellipse(c, Vector2(0, r * 0.22), r * 0.5, r * 0.36, MOLE_LIGHT)
	# 護目鏡
	c.draw_line(Vector2(-r * 0.98, -r * 0.38), Vector2(r * 0.98, -r * 0.38), INK, 4.0)
	for s in [-1.0, 1.0]:
		var eye := Vector2(s * r * 0.38, -r * 0.38)
		c.draw_circle(eye, r * 0.27 + 2.5, INK)
		c.draw_circle(eye, r * 0.27, GOGGLE)
		if dizzy:
			c.draw_arc(eye, r * 0.15, 0.0, TAU * 0.75, 12, INK, 2.0, true)
			c.draw_arc(eye, r * 0.07, PI, TAU * 1.2, 8, INK, 2.0, true)
		else:
			c.draw_circle(eye + Vector2(r * 0.04, r * 0.04), r * 0.1, INK)
	c.draw_circle(Vector2(0, r * 0.1), r * 0.13, BEAN)
	# 大門牙
	c.draw_rect(Rect2(-r * 0.13, r * 0.28, r * 0.26, r * 0.2), INK)
	c.draw_rect(Rect2(-r * 0.10, r * 0.28, r * 0.20, r * 0.16), Color.WHITE)
	# 爪子扒著洞口
	for s2 in [-1.0, 1.0]:
		var paw := Vector2(s2 * r * 0.72, r * 0.6)
		c.draw_circle(paw, r * 0.2 + 2.5, INK)
		c.draw_circle(paw, r * 0.2, MOLE_LIGHT)


## 地洞。open = 還開著（球會掉進去）；填平後只剩一塊淡淡的土
static func mole_hole(c: CanvasItem, r: float, open: bool, flash: float, hp: int, max_hp: int) -> void:
	if not open:
		_ellipse(c, Vector2.ZERO, r * 0.95, r * 0.6, Color(DIRT, 0.45))
		for k in 3:
			c.draw_circle(Vector2((k - 1) * r * 0.4, (k % 2) * r * 0.15 - r * 0.05), r * 0.1, Color(DIRT_DARK, 0.35))
		return
	if _disc_sprite(c, "hole", r * 1.1):
		_hit_glow(c, r, flash)
	else:
		_ellipse(c, Vector2.ZERO, r * 1.15 + 3.0, r * 0.82 + 3.0, INK)
		_ellipse(c, Vector2.ZERO, r * 1.15, r * 0.82, DIRT.lerp(Color.WHITE, flash * 0.4))
		_ellipse(c, Vector2(0, r * 0.06), r * 0.66, r * 0.46, DIRT_DARK)
	# 還要填幾下：洞口上方的小點
	for i in max_hp:
		var p := Vector2((i - (max_hp - 1) * 0.5) * r * 0.42, -r * 1.3)
		c.draw_circle(p, r * 0.12 + 1.5, INK)
		c.draw_circle(p, r * 0.12, BELL if i < hp else Color(INK, 0.3))


## 旋轉灑水器（第二關）。節點本身在轉，這裡只畫「轉了幾度的樣子」。t 用來讓水花動起來
static func sprinkler(c: CanvasItem, arm: float, t: float) -> void:
	var water := Color(0.62, 0.85, 0.98)
	# 水花：從兩端的噴頭往轉動的反方向甩出去
	for s in [-1.0, 1.0]:
		var tip := Vector2(s * arm, 0.0)
		for k in 5:
			var ph := fmod(t * 2.4 + k * 0.2, 1.0)
			var p := tip + Vector2(s * 16.0 * ph, -s * 34.0 * ph)
			c.draw_circle(p, 3.5 + 3.0 * (1.0 - ph), Color(water, 0.75 * (1.0 - ph)))
	var tex := _art_tex("sprinkler")
	var m: Dictionary = _art_meta("sprinkler").get("members", {}).get("sprinkler", {})
	if tex != null and not m.is_empty():
		# 圖的噴頭尖端對齊程式版噴頭的外緣（arm + 8.5）
		var k := (arm + 8.5) / float(m.get("half_len_px", 1.0))
		var ctr := _vec(m.get("center", [0.0, 0.0]))
		c.draw_texture_rect(tex, Rect2(-ctr * k, Vector2(tex.get_width(), tex.get_height()) * k), false)
		return
	# 桿子
	c.draw_line(Vector2(-arm, 0), Vector2(arm, 0), INK, 16.0)
	c.draw_line(Vector2(-arm, 0), Vector2(arm, 0), LID, 8.0)
	for s2 in [-1.0, 1.0]:
		c.draw_circle(Vector2(s2 * arm, 0), 8.5, INK)
		c.draw_circle(Vector2(s2 * arm, 0), 5.5, water)
	# 中心
	c.draw_circle(Vector2.ZERO, 16.5, INK)
	c.draw_circle(Vector2.ZERO, 14.0, LEAF)
	c.draw_circle(Vector2.ZERO, 4.5, INK)


## 土堆（第二關第二階段）。grow：冒出來 0→1；fade：快消失時 1→0
static func mound(c: CanvasItem, r: float, grow: float, fade: float) -> void:
	var s := 0.4 + 0.6 * grow
	if _disc_sprite(c, "mound", r * 1.05 * s, Vector2(0, r * 0.1), Color(1, 1, 1, fade)):
		return
	_ellipse(c, Vector2(0, r * 0.15), r * 1.1 * s + 3.0, r * 0.85 * s + 3.0, Color(INK, fade))
	_ellipse(c, Vector2(0, r * 0.15), r * 1.1 * s, r * 0.85 * s, Color(DIRT, fade))
	_ellipse(c, Vector2(-r * 0.25, -r * 0.1) * s, r * 0.5 * s, r * 0.3 * s, Color(DIRT.lightened(0.2), fade))
	for k in 3:
		c.draw_circle(Vector2((k - 1) * r * 0.45, r * 0.45) * s, r * 0.1 * s, Color(DIRT_DARK, fade))


## 被小刺蝟刺到的砲台（第三關）：頭上插著幾根刺；外圈一條細弧是剩下的時間（left 1 → 0）
static func thorns(c: CanvasItem, r: float, left: float) -> void:
	for k in 5:
		var a := -PI * 0.5 + (k - 2) * 0.38
		var dir := Vector2(cos(a), sin(a))
		c.draw_line(dir * r * 0.8, dir * r * 1.4, INK, 4.5)
		c.draw_line(dir * r * 0.8, dir * r * 1.4, HEDGEHOG, 2.0)
	c.draw_arc(Vector2.ZERO, r + 13.0, -PI * 0.5, -PI * 0.5 + TAU * clampf(left, 0.0, 1.0), 32,
		Color(DANGER, 0.55), 2.5, true)


## 砲台的蓄力進度（要打好幾下才開砲時）
static func charge_ring(c: CanvasItem, r: float, ratio: float) -> void:
	var a0 := -PI * 0.5
	c.draw_arc(Vector2.ZERO, r + 6.0, a0, a0 + TAU * ratio, 24, INK, 6.5, true)
	c.draw_arc(Vector2.ZERO, r + 6.0, a0, a0 + TAU * ratio, 24, BELL, 3.5, true)


## 草叢小徑的短柱：跟牆一樣的輪廓＋填色
static func lane_post(c: CanvasItem, a: Vector2, b: Vector2) -> void:
	c.draw_line(a, b, INK, 16.0)
	c.draw_circle(a, 8.0, INK)
	c.draw_circle(b, 8.0, INK)
	c.draw_line(a, b, WALL_FILL, 7.0)
	c.draw_circle(a, 3.5, WALL_FILL)
	c.draw_circle(b, 3.5, WALL_FILL)


## 草叢小徑的燈：lit = 亮（黃色帶光暈），flash = 剛被點亮
static func lane_light(c: CanvasItem, at: Vector2, lit: bool, flash: float) -> void:
	for s in [-1.0, 1.0]:
		c.draw_line(at + Vector2(s * 14.0, 14.0), at + Vector2(s * 21.0, -2.0), LEAF_DARK, 3.0)
		c.draw_line(at + Vector2(s * 9.0, 16.0), at + Vector2(s * 12.0, 4.0), LEAF, 3.0)
	if lit:
		c.draw_circle(at, 20.0 + 6.0 * flash, Color(BELL, 0.28))
	c.draw_circle(at, 15.5, INK)
	c.draw_circle(at, 13.0, BELL.lerp(Color.WHITE, flash * 0.6) if lit else Color(0.80, 0.78, 0.72))


## 泥巴球（阿吉丟的）。size ≈ 半徑
static func mud_ball(c: CanvasItem, at: Vector2, rot: float, size: float) -> void:
	c.draw_set_transform(at, rot, Vector2.ONE)
	c.draw_circle(Vector2.ZERO, size + 2.0, INK)
	c.draw_circle(Vector2.ZERO, size, DIRT)
	c.draw_circle(Vector2(-size * 0.3, -size * 0.3), size * 0.35, DIRT.lightened(0.2))
	c.draw_circle(Vector2(size * 0.35, size * 0.2), size * 0.22, DIRT_DARK)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 被打跑的松鼠：原地留一小團煙
static func part_gone(c: CanvasItem, r: float) -> void:
	for i in 3:
		var a: float = TAU * i / 3.0 + 0.4
		c.draw_circle(Vector2(r * 0.30, 0).rotated(a), r * 0.26, Color(INK, 0.10))
	c.draw_arc(Vector2.ZERO, r * 0.55, 0.0, TAU, 20, Color(INK, 0.22), 2.0, true)


## 預警：松鼠身上的紅色脈動圈
static func throw_charge(c: CanvasItem, at: Vector2, r: float, pulse: float) -> void:
	c.draw_circle(at, r * (1.0 + 0.10 * pulse), Color(DANGER, 0.10 + 0.10 * pulse))
	c.draw_arc(at, r + 10.0 + 6.0 * pulse, 0.0, TAU, 40, Color(DANGER, 0.55 + 0.35 * pulse), 3.0, true)


# ── 敵人的攻擊 = 橡實 ───────────────────────────────────────

## size ≈ 橡實的半徑
static func acorn(c: CanvasItem, at: Vector2, rot: float, size: float) -> void:
	c.draw_set_transform(at, rot, Vector2.ONE)
	_ellipse(c, Vector2(0, size * 0.20), size * 0.80 + 2.0, size + 2.0, INK)
	_ellipse(c, Vector2(0, size * 0.20), size * 0.80, size, ACORN)
	_ellipse(c, Vector2(0, -size * 0.55), size * 0.95 + 2.0, size * 0.50 + 2.0, INK)
	_ellipse(c, Vector2(0, -size * 0.55), size * 0.95, size * 0.50, ACORN_CAP)
	c.draw_line(Vector2(0, -size * 1.0), Vector2(size * 0.18, -size * 1.45), INK, 2.0)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 預警瞄準線：從松鼠拉一條紅色虛線到即將被砸的目標
static func attack_aim(c: CanvasItem, from: Vector2, to: Vector2, pulse: float) -> void:
	var d := to - from
	var n := maxi(int(d.length() / 14.0), 2)
	for i in range(0, n, 2):
		c.draw_line(from + d * (float(i) / n), from + d * (float(i + 1) / n),
			Color(DANGER, 0.45 + 0.5 * pulse), 4.0)
	c.draw_arc(to, 26.0 + 11.0 * pulse, 0.0, TAU, 28, Color(DANGER, 0.5 + 0.45 * pulse), 3.0, true)
	c.draw_circle(to, 5.0 + 3.0 * pulse, Color(DANGER, 0.7 + 0.3 * pulse))


# ── 第 3 關的敵人 = 巴哥老大＋紙箱城牆 ─────────────────────

## 巴哥老大。armored = 紙箱城牆還擋著（打不動）；dizzy = 第一條血打完暈頭；
## stomping = 重踏步預警（抬腳、紅圈）；angry = 第二階段（眉毛倒豎、邊跑邊跳）
static func pug(c: CanvasItem, r: float, armored: bool, dizzy: bool, flash: float, hp: int, max_hp: int,
		wind_up: float, stomping: bool, angry: bool) -> void:
	var t := Time.get_ticks_msec() / 1000.0
	var at := Vector2.ZERO
	if stomping:
		var pulse := 0.5 + 0.5 * sin(t * 18.0)
		c.draw_circle(Vector2.ZERO, r * (1.05 + 0.1 * pulse), Color(DANGER, 0.10 + 0.12 * pulse))
		c.draw_arc(Vector2.ZERO, r + 14.0 + 6.0 * pulse, 0.0, TAU, 40, Color(DANGER, 0.5 + 0.4 * pulse), 4.0, true)
		at = Vector2(0.0, -r * 0.22)
	elif angry:
		at = Vector2(0.0, -absf(sin(t * 9.0)) * r * 0.12)
	var key := "pug_dizzy" if dizzy else ("pug_angry" if angry else "pug")
	if not _disc_sprite(c, key, r, at, Color.WHITE.lerp(HURT, flash), 1.0, "pug"):
		_pug_body(c, r, at, dizzy, angry, flash)
	# 抬起來準備踩下去的腳
	if stomping:
		var foot := at + Vector2(r * 0.95, r * 0.15)
		c.draw_circle(foot, r * 0.32 + 2.5, INK)
		c.draw_circle(foot, r * 0.32, PUG)
		for k in 3:
			c.draw_line(foot + Vector2((k - 1) * r * 0.12, r * 0.12), foot + Vector2((k - 1) * r * 0.12, r * 0.3), INK, 2.0)
	# 預警：玩具球（第二階段是骨頭）舉高
	if wind_up > 0.0:
		var hold := at + Vector2(-r * 0.95, -r * (0.95 + 0.3 * wind_up))
		var s := r * 0.3 * (1.0 + 0.3 * wind_up)
		if angry:
			bone(c, hold, 0.4, s)
		else:
			toy_ball(c, hold, 0.0, s)
	if dizzy:
		var spin := t * 2.5
		for i in 3:
			var a := spin + TAU * i / 3.0
			var p := at + Vector2(cos(a) * r * 0.8, -r * 1.1 + sin(a) * r * 0.2)
			c.draw_circle(p, r * 0.12 + 1.5, INK)
			c.draw_circle(p, r * 0.12, BELL)
	if not armored:
		_hp_ring(c, r + 9.0, hp, max_hp, 4.0)


static func _pug_body(c: CanvasItem, r: float, at: Vector2, dizzy: bool, angry: bool, flash: float) -> void:
	var fur := PUG.lerp(HURT, flash)
	# 往下垂的黑耳朵
	for s in [-1.0, 1.0]:
		_ellipse(c, at + Vector2(s * r * 0.82, -r * 0.58), r * 0.34 + 2.5, r * 0.22 + 2.5, INK, s * 0.7)
		_ellipse(c, at + Vector2(s * r * 0.82, -r * 0.58), r * 0.34, r * 0.22, PUG_MASK, s * 0.7)
	c.draw_circle(at, r + 3.0, INK)
	c.draw_circle(at, r, fur)
	# 額頭皺紋
	for k in 2:
		c.draw_arc(at + Vector2(0.0, -r * (0.18 + k * 0.16)), r * 0.34, PI * 1.2, PI * 1.8, 10, PUG_SHADE, 2.5, true)
	# 黑色口鼻、鼻子、吐一點舌頭
	_ellipse(c, at + Vector2(0.0, r * 0.34), r * 0.62 + 2.0, r * 0.46 + 2.0, INK)
	_ellipse(c, at + Vector2(0.0, r * 0.34), r * 0.62, r * 0.46, PUG_MASK)
	_ellipse(c, at + Vector2(0.0, r * 0.16), r * 0.2, r * 0.12, INK)
	c.draw_line(at + Vector2(0.0, r * 0.26), at + Vector2(0.0, r * 0.46), Color(fur, 0.6), 2.0)
	# 倔強的下排小虎牙
	for s2 in [-1.0, 1.0]:
		var fang := at + Vector2(s2 * r * 0.17, r * 0.56)
		c.draw_colored_polygon(PackedVector2Array([
			fang + Vector2(-r * 0.07, r * 0.05), fang + Vector2(r * 0.07, r * 0.05),
			fang + Vector2(0.0, -r * 0.11)]), Color.WHITE)
	# 眼睛
	for s in [-1.0, 1.0]:
		var e := at + Vector2(s * r * 0.42, -r * 0.08)
		c.draw_circle(e, r * 0.21 + 2.0, INK)
		c.draw_circle(e, r * 0.21, Color.WHITE)
		if dizzy:
			c.draw_arc(e, r * 0.12, 0.0, TAU * 0.8, 10, INK, 2.0, true)
		else:
			c.draw_circle(e + Vector2(0.0, r * 0.04), r * 0.11, INK)
		# 平常就是臭臉：皺著眉瞪人。第二階段眉毛更斜
		var brow := 1.0 if angry else 0.55
		c.draw_line(e + Vector2(s * r * 0.3, -r * (0.2 + 0.14 * brow)),
			e + Vector2(-s * r * 0.2, -r * (0.2 + 0.02 * brow)), INK, 3.5)
	# 台味大王配備：尖刺項圈＋粗金鍊金牌
	var y := r * 0.86
	# 金鍊掛在最下面
	for k in 7:
		var link := at + Vector2((k - 3) * r * 0.18, y + r * 0.5 - absf(k - 3) * r * 0.03)
		c.draw_circle(link, r * 0.08 + 1.5, INK)
		c.draw_circle(link, r * 0.08, GOLD)
	c.draw_circle(at + Vector2(0.0, y + r * 0.66), r * 0.15 + 2.0, INK)
	c.draw_circle(at + Vector2(0.0, y + r * 0.66), r * 0.15, GOLD)
	c.draw_circle(at + Vector2(0.0, y + r * 0.66), r * 0.075, GOLD_DARK)
	# 項圈本體
	c.draw_line(at + Vector2(-r * 0.8, y), at + Vector2(r * 0.8, y), INK, r * 0.34)
	c.draw_line(at + Vector2(-r * 0.78, y), at + Vector2(r * 0.78, y), COLLAR, r * 0.26)
	# 朝下露出來的銀色尖刺
	for k in 6:
		var sx := (k - 2.5) * r * 0.27
		var top := y + r * 0.1
		c.draw_colored_polygon(PackedVector2Array([
			at + Vector2(sx - r * 0.085, top), at + Vector2(sx + r * 0.085, top),
			at + Vector2(sx, top + r * 0.3)]), INK)
		c.draw_colored_polygon(PackedVector2Array([
			at + Vector2(sx - r * 0.055, top), at + Vector2(sx + r * 0.055, top),
			at + Vector2(sx, top + r * 0.24)]), STUD)


## 紙箱城牆的一格。被打過會出現裂痕、壓扁一點，上面的小點是還要打幾下
static func cardboard_box(c: CanvasItem, r: float, flash: float, hp: int, max_hp: int) -> void:
	# 打過一下就換成爛爛的那張
	var key := "castle_box" if hp >= max_hp else "castle_box_hit"
	if _disc_sprite(c, key, r, Vector2.ZERO, Color.WHITE.lerp(HURT, flash), 1.0, "castle_box"):
		pass
	else:
		var dmg := 1.0 - float(hp) / float(maxi(max_hp, 1))
		var rect := Rect2(-r, -r * (0.9 - 0.2 * dmg), r * 2.0, r * (1.8 - 0.2 * dmg))
		c.draw_rect(rect.grow(3.0), INK)
		c.draw_rect(rect, BOX.lerp(Color.WHITE, flash * 0.45))
		c.draw_rect(Rect2(rect.position.x + rect.size.x * 0.62, rect.position.y, rect.size.x * 0.38, rect.size.y),
			BOX_SHADE.lerp(Color.WHITE, flash * 0.45))
		# 蓋子的縫、膠帶
		c.draw_line(Vector2(rect.position.x, rect.position.y + r * 0.4), Vector2(rect.end.x, rect.position.y + r * 0.4), INK, 2.0)
		c.draw_rect(Rect2(-r * 0.14, rect.position.y, r * 0.28, r * 0.4), Color(0.93, 0.87, 0.70))
		if dmg > 0.0:
			c.draw_polyline(PackedVector2Array([
				Vector2(-r * 0.62, rect.position.y + r * 0.55), Vector2(-r * 0.3, r * 0.1),
				Vector2(-r * 0.5, r * 0.42), Vector2(-r * 0.18, rect.end.y)]), INK, 2.5)
	for i in max_hp:
		var p := Vector2((i - (max_hp - 1) * 0.5) * r * 0.5, -r * 1.2)
		c.draw_circle(p, r * 0.13 + 1.5, INK)
		c.draw_circle(p, r * 0.13, BELL if i < hp else Color(INK, 0.3))


## 玩具球（巴哥丟的；也是第三關障礙物皮球的畫法）。size ≈ 半徑
static func toy_ball(c: CanvasItem, at: Vector2, rot: float, size: float) -> void:
	c.draw_set_transform(at, rot, Vector2.ONE)
	c.draw_circle(Vector2.ZERO, size + 2.0, INK)
	c.draw_circle(Vector2.ZERO, size, TOY_YELLOW)
	for k in 2:
		var a0 := -PI / 3.0 + k * PI
		var wedge := PackedVector2Array([Vector2.ZERO])
		for i in 7:
			wedge.append(Vector2(size, 0.0).rotated(a0 + (PI * 2.0 / 3.0) * i / 6.0))
		c.draw_colored_polygon(wedge, TOY_RED if k == 0 else TOY_BLUE)
	c.draw_circle(Vector2(-size * 0.35, -size * 0.38), size * 0.22, Color(1, 1, 1, 0.5))
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 骨頭（第二階段的巴哥丟的）。size ≈ 半長
static func bone(c: CanvasItem, at: Vector2, rot: float, size: float) -> void:
	c.draw_set_transform(at, rot, Vector2.ONE)
	for pass_i in 2:
		var g := 2.5 if pass_i == 0 else 0.0
		var col := INK if pass_i == 0 else BONE
		c.draw_rect(Rect2(-size * 0.9 - g, -size * 0.26 - g, size * 1.8 + g * 2.0, size * 0.52 + g * 2.0), col)
		for sx in [-1.0, 1.0]:
			for sy in [-1.0, 1.0]:
				c.draw_circle(Vector2(sx * size * 0.95, sy * size * 0.28), size * 0.32 + g, col)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 小刺蝟縮成的刺球（戴黃色安全帽）。rot = 滾動角度；knocked = 被球打中、正在彈回推車
static func hedgehog(c: CanvasItem, r: float, rot: float, knocked: bool) -> void:
	# 有圖就用圖。刺球會滾，所以自己套旋轉，不走 _disc_sprite
	var tex := _art_tex("hedgehog")
	var m: Dictionary = _art_meta("hedgehog").get("members", {}).get("hedgehog", {})
	if tex != null and not m.is_empty():
		var k := r / float(m.get("radius_px", 1.0))
		var ctr := _vec(m.get("center", [0.0, 0.0]))
		# 圖上有臉和安全帽，整圈轉會頭下腳上：改成滾動時左右晃
		c.draw_set_transform(Vector2.ZERO, sin(rot) * 0.22, Vector2.ONE)
		c.draw_texture_rect(tex, Rect2(-ctr * k, Vector2(tex.get_width(), tex.get_height()) * k), false)
		c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
		return
	c.draw_set_transform(Vector2.ZERO, rot, Vector2.ONE)
	var spikes := PackedVector2Array()
	var outline := PackedVector2Array()
	for i in 28:
		var p := Vector2.RIGHT.rotated(TAU * i / 28.0) * (r * (1.22 if i % 2 == 0 else 0.9))
		spikes.append(p)
		outline.append(p * ((r + 3.0) / r))
	c.draw_colored_polygon(outline, INK)
	c.draw_colored_polygon(spikes, HEDGEHOG)
	c.draw_circle(Vector2.ZERO, r * 0.66, HEDGEHOG_DARK)
	# 縮成一團，只露出一點臉
	var face := Vector2(r * 0.22, r * 0.2)
	c.draw_circle(face, r * 0.44 + 2.0, INK)
	c.draw_circle(face, r * 0.44, HEDGEHOG_FACE)
	c.draw_circle(face + Vector2(r * 0.3, r * 0.06), r * 0.1, INK)
	var eye := face + Vector2(r * 0.02, -r * 0.1)
	if knocked:
		c.draw_line(eye + Vector2(-3, -3), eye + Vector2(3, 3), INK, 2.0)
		c.draw_line(eye + Vector2(-3, 3), eye + Vector2(3, -3), INK, 2.0)
	else:
		c.draw_circle(eye, r * 0.08, INK)
	# 安全帽
	var helm := PackedVector2Array()
	for i in 9:
		helm.append(Vector2(-r * 0.12, -r * 0.18) + Vector2(r * 0.64, 0.0).rotated(PI + PI * i / 8.0))
	c.draw_colored_polygon(helm, HELMET)
	helm.append(helm[0])
	c.draw_polyline(helm, INK, 2.5)
	c.draw_line(Vector2(-r * 0.9, -r * 0.18), Vector2(r * 0.66, -r * 0.18), INK, 3.0)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 小刺蝟的小推車（第三關，裝飾）。flip = -1 左右翻面（右邊那台）
static func cart(c: CanvasItem, flip: float) -> void:
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2(flip, 1.0))
	c.draw_line(Vector2(28.0, -16.0), Vector2(46.0, -36.0), INK, 5.0)
	c.draw_line(Vector2(40.0, -36.0), Vector2(52.0, -36.0), INK, 5.0)
	var body := Rect2(-32.0, -24.0, 64.0, 32.0)
	c.draw_rect(body.grow(3.0), INK)
	c.draw_rect(body, TOY_RED)
	c.draw_rect(Rect2(body.position.x, body.position.y, body.size.x, 9.0), TOY_RED.darkened(0.18))
	for x in [-18.0, 18.0]:
		c.draw_circle(Vector2(x, 12.0), 9.5, INK)
		c.draw_circle(Vector2(x, 12.0), 5.5, Color(0.72, 0.72, 0.70))
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 第三關的障礙物 = 搶來的大皮球（會彈球、不開砲）
static func beach_ball(c: CanvasItem, r: float, heat: float) -> void:
	if _disc_sprite(c, "beachball", r):
		_hit_glow(c, r, heat)
		return
	toy_ball(c, Vector2.ZERO, 0.4, r)
	if heat > 0.0:
		c.draw_circle(Vector2.ZERO, r, Color(1, 1, 1, heat * 0.35))


# ── 子彈 = 小魚乾 ───────────────────────────────────────────

static func fish(c: CanvasItem, dir: Vector2, size: float) -> void:
	if _fish_sprite(c, dir, size):
		return
	var rot := dir.angle()
	c.draw_set_transform(Vector2.ZERO, rot, Vector2(1.0, 0.62))
	c.draw_circle(Vector2.ZERO, size, FISH)
	c.draw_set_transform(Vector2.ZERO, rot, Vector2.ONE)
	c.draw_colored_polygon(PackedVector2Array([
		Vector2(-size * 0.75, 0), Vector2(-size * 1.85, -size * 0.80),
		Vector2(-size * 1.85, size * 0.80)]), FISH)
	c.draw_circle(Vector2(size * 0.42, -size * 0.18), size * 0.18, Color(0.1, 0.15, 0.2))
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)


## 小魚乾的圖（魚頭朝右）。art/fish.json 是主軸量測：pivots.fish 是尾端，
## 沿 angle_deg 走 length_px 到魚頭。魚身中點對齊原點，轉到飛行方向，全長縮成 size × 3.7。
static func _fish_sprite(c: CanvasItem, dir: Vector2, size: float) -> bool:
	var tex := _art_tex("fish")
	var m := _art_meta("fish")
	if tex == null or not m.has("length_px"):
		return false
	var art_len := float(m["length_px"])
	var art_angle := deg_to_rad(float(m.get("angle_deg", 0.0)))
	var mid := _vec(m.get("pivots", {}).get("fish", [0.0, 0.0])) \
		+ Vector2(cos(art_angle), sin(art_angle)) * art_len * 0.5
	var k := size * 3.7 / art_len
	var xf := Transform2D(dir.angle(), Vector2.ZERO) \
		* Transform2D(Vector2(k, 0.0), Vector2(0.0, k), Vector2.ZERO) \
		* Transform2D(-art_angle, Vector2.ZERO)
	c.draw_set_transform_matrix(xf)
	c.draw_texture_rect(tex, Rect2(-mid, Vector2(tex.get_width(), tex.get_height())), false)
	c.draw_set_transform_matrix(Transform2D.IDENTITY)
	return true


# ── 發射道的彈簧 ────────────────────────────────────────────

## top：擋板頂端高度；floor_y：彈簧底座；charge：蓄力（壓越深擋板越黃）
static func plunger(c: CanvasItem, x: float, top: float, floor_y: float, charge: float) -> void:
	var bottom := top + 10.0
	var coils := 5
	var pts := PackedVector2Array()
	for i in coils * 2 + 1:
		var t := float(i) / float(coils * 2)
		var dx := 0.0 if i == 0 or i == coils * 2 else (-11.0 if i % 2 == 1 else 11.0)
		pts.append(Vector2(x + dx, lerpf(floor_y, bottom, t)))
	c.draw_polyline(pts, INK, 6.0)
	c.draw_polyline(pts, LID, 2.5)
	var plate := Rect2(x - 20.0, top, 40.0, 10.0)
	c.draw_rect(plate.grow(2.5), INK)
	c.draw_rect(plate, WALL_FILL.lerp(BELL, charge))
	c.draw_circle(Vector2(x, top + 5.0), 3.0, BEAN)


# ── HUD ─────────────────────────────────────────────────────

static func yarn_icon(c: CanvasItem, pos: Vector2, r: float, filled: bool) -> void:
	if filled and _disc_sprite(c, "yarn", r, pos):
		return
	if filled:
		c.draw_circle(pos, r, YARN)
		c.draw_circle(pos + Vector2(-r * 0.3, -r * 0.3), r * 0.55, YARN_LIGHT)
		c.draw_arc(pos, r * 0.92, 0.0, TAU, 20, YARN_DARK, 1.6, true)
		c.draw_line(pos + Vector2(-r * 0.8, r * 0.3), pos + Vector2(r * 0.8, -r * 0.3), YARN_DARK, 1.6)
	else:
		c.draw_arc(pos, r, 0.0, TAU, 20, Color(INK, 0.55), 2.0, true)


# ── 共用小工具 ──────────────────────────────────────────────

## 血量環：一格一段。底下墊一圈輪廓色，淺色地板上才看得清楚。
static func _hp_ring(c: CanvasItem, radius: float, hp: int, max_hp: int, width: float) -> void:
	if max_hp <= 0:
		return
	var step := TAU / float(max_hp)
	var gap := 4.0 / radius
	for i in hp:
		c.draw_arc(Vector2.ZERO, radius, i * step + gap, (i + 1) * step - gap, 12, INK, width + 3.0, true)
		c.draw_arc(Vector2.ZERO, radius, i * step + gap, (i + 1) * step - gap, 12, BELL, width, true)


## 被撞到：蓋一層暖白光，很快淡掉
static func _hit_glow(c: CanvasItem, r: float, heat: float) -> void:
	if heat > 0.0:
		c.draw_circle(Vector2.ZERO, r, Color(1.0, 0.96, 0.82, 0.55 * heat))


## 貓耳（尖的），畫在頭的圓後面
static func _cat_ears(c: CanvasItem, r: float, fur: Color, inner: Color) -> void:
	for s in [-1.0, 1.0]:
		var b := Vector2(s * r * 0.52, -r * 0.72)
		var ear := PackedVector2Array([
			b + Vector2(-r * 0.28, r * 0.24),
			b + Vector2(s * r * 0.30, -r * 0.46),
			b + Vector2(r * 0.28, r * 0.24)])
		c.draw_colored_polygon(ear, fur)
		ear.append(ear[0])
		c.draw_polyline(ear, INK, 4.0, true)
		c.draw_colored_polygon(PackedVector2Array([
			b + Vector2(-r * 0.15, r * 0.18),
			b + Vector2(s * r * 0.18, -r * 0.24),
			b + Vector2(r * 0.15, r * 0.18)]), inner)


## 實心橢圓。rot 是橢圓本身的旋轉角
static func _ellipse(c: CanvasItem, center: Vector2, rx: float, ry: float, color: Color,
		rot: float = 0.0) -> void:
	var pts := PackedVector2Array()
	for i in 24:
		var a := TAU * i / 24.0
		pts.append(center + Vector2(cos(a) * rx, sin(a) * ry).rotated(rot))
	c.draw_colored_polygon(pts, color)


static func _vec(a: Variant) -> Vector2:
	return Vector2(float(a[0]), float(a[1]))


# ── 正式美術：元件共用的讀圖 ────────────────────────────────
# 圖是 res://art/<key>.png，數字是 tools/key_green.py 量出的 res://art/<群組>.json。
# 結果都會快取，不會每幀查檔案。

static var _tex_cache: Dictionary = {}
static var _meta_cache: Dictionary = {}


## 讀 key_green.py 量出的 json。沒有檔案或格式不對時回傳空的，呼叫端就退回程式繪圖。
static func _read_json(path: String) -> Dictionary:
	if not FileAccess.file_exists(path):
		return {}
	var parsed: Variant = JSON.parse_string(FileAccess.get_file_as_string(path))
	if typeof(parsed) != TYPE_DICTIONARY:
		push_warning("Formato de %s no válido; se usará el dibujo de reserva" % path)
		return {}
	return parsed


static func _art_tex(key: String) -> Texture2D:
	if not _tex_cache.has(key):
		var path := "res://art/%s.png" % key
		_tex_cache[key] = load(path) if ResourceLoader.exists(path) else null
	return _tex_cache[key]


static func _art_meta(key: String) -> Dictionary:
	if not _meta_cache.has(key):
		_meta_cache[key] = _read_json("res://art/%s.json" % key)
	return _meta_cache[key]


## 圓形元件：圖上量出的圓（center、radius_px）對齊 at、縮放成半徑 r。
## 圓外的部分（耳朵、尾巴、舉起的橡實）照比例畫在圓外。mirror = -1 左右翻面。
## group：同一個元件有多個狀態時（例如浣熊舉鍋蓋／鍋蓋掉了），量測數字存在群組名稱的 json 裡。
static func _disc_sprite(c: CanvasItem, key: String, r: float, at := Vector2.ZERO,
		tint := Color.WHITE, mirror := 1.0, group := "") -> bool:
	var tex := _art_tex(key)
	var m: Dictionary = _art_meta(group if group != "" else key).get("members", {}).get(key, {})
	if tex == null or m.is_empty():
		return false
	var k := r / float(m.get("radius_px", 1.0))
	var ctr := _vec(m.get("center", [0.0, 0.0]))
	c.draw_set_transform(at, 0.0, Vector2(mirror, 1.0))
	c.draw_texture_rect(tex, Rect2(-ctr * k, Vector2(tex.get_width(), tex.get_height()) * k), false, tint)
	c.draw_set_transform(Vector2.ZERO, 0.0, Vector2.ONE)
	return true
