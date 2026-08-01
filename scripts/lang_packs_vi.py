# -*- coding: utf-8 -*-
"""Vietnamese complete UI pack."""
from __future__ import print_function

# Import shared builder: copy structure from a complete sibling via manual full dict.
# Full VI translations for all non-CHANGELOG keys.
VI = {}

# Build by reading en and applying translations from this table.
_VI_LINES = r"""
MOD_DESCRIPTION Giao thông công cộng cải tiến: điều khiển tuyến, đội xe, tích hợp và hơn thế.
CURRENT_WEEK Tuần hiện tại
LAST_WEEK Tuần trước
AVERAGE Trung bình
AVERAGE_TOOLTIP Trung bình của {0} tuần gần nhất.
CITY_SERVICE_PANEL_TITLE_STATION_STOPS Điểm dừng ga
CITY_SERVICE_PANEL_TITLE_DEPOT_VEHICLES Xe trong depot
CITYSERVICE_ACCEPTINTERCITYBUSES Cho phép xe buýt liên tỉnh
CITYSERVICE_ACCEPTINTERCITYBUSES_TOOLTIP Cho phép xe buýt liên tỉnh dùng ga này. Tắt để chỉ xe buýt nội thành.
EXPLANATION_BUDGET_CONTROL Kiểm soát ngân sách: Số xe do ngân sách điều khiển.
EXPLANATION_UNBUNCHING Chống dồn xe: Trò chơi cố tạo khoảng cách giữa các xe.
LINE_PANEL_STOPS Điểm dừng: {0}
LINE_PANEL_SPAWNTIMER Xe tiếp theo sau {0} giây.
LINE_PANEL_DEPOT_WARNING <color #FF0000>Depot đã chọn không còn xe.</color>
LINE_PANEL_BUDGET_CONTROL Kiểm soát ngân sách
LINE_PANEL_BUDGET_CONTROL_TOOLTIP Bật hoặc tắt kiểm soát ngân sách cho tuyến này.
LINE_PANEL_UNBUNCHING_TOOLTIP Bật hoặc tắt chống dồn xe cho tuyến này.\nChống dồn tắt nếu độ mạnh = 0.
LINE_PANEL_DEPOT Depot:
LINE_PANEL_NO_DEPOT_FOUND Không tìm thấy depot.
LINE_PANEL_DEPOT_MARKER_TOOLTIP Nhảy tới depot đã chọn.\nGiữ Shift khi nhấp để phóng to.
LINE_PANEL_SELECT_TYPES Chọn loại
LINE_PANEL_SELECT_TYPES_TOOLTIP Bật/tắt bảng 'Chọn loại'.\nNếu nút tắt, bạn phải chọn depot trước.
LINE_PANEL_LINE_STOPS Điểm dừng tuyến
LINE_PANEL_LINE_VEHICLES Xe trên tuyến này
LINE_PANEL_ENQUEUED Xe trong hàng đợi
LINE_PANEL_TOTAL_WAITING_PEOPLE_TOOLTIP {0} hành khách đang chờ trên tuyến này.
LINE_PANEL_ADD_VEHICLE Thêm xe
LINE_PANEL_ADD_VEHICLE_TOOLTIP Thêm xe mới vào tuyến.\nNếu nút tắt, depot đã chọn không còn xe.
LINE_PANEL_REMOVE_VEHICLE Gỡ xe
STOP_LIST_BOX_ROW_STOP Điểm dừng #{0}
STOP_LIST_BOX_ROW_TOOLTIP {0}\nHành khách chờ: {1}\n\nNhấp phải để nhảy tới điểm dừng này.\nGiữ Shift khi nhấp để phóng to.
STOP_PANEL_SUGGESTED_NAMES_TOOLTIP Danh sách tên điểm dừng gợi ý.
STOP_PANEL_REUSE_NAME_TOOLTIP Đặt tên này cho mọi điểm dừng khác tại ga/vị trí này.
STOP_PANEL_WAITING_PEOPLE Hành khách chờ: {0}
COMMUTER_DESTINATION_PANEL_TITLE Điểm đến hành khách
COMMUTER_DESTINATION_HEADER Điểm đến hàng đầu:
COMMUTER_DESTINATION_NONE Hiện không có hành khách chờ ở đây.
COMMUTER_DESTINATION_LOADING Đang tính...
COMMUTER_DESTINATION_BUTTON Điểm đến
COMMUTER_DESTINATION_BUTTON_TOOLTIP Hiện hành khách chờ tại điểm dừng này đang đi đâu.
STOP_PANEL_BORED_TIMER Thời gian đến khi chán: <color #{0}>{1}</color>
STOP_PANEL_BORED_TIMER_TOOLTIP Hành khách rời điểm dừng khi đếm ngược về 0.
STOP_PANEL_PASSENGERS_IN Hành khách lên:
STOP_PANEL_PASSENGERS_IN_TOOLTIP Hành khách lên xe tại đây.
STOP_PANEL_PASSENGERS_OUT Hành khách xuống:
STOP_PANEL_PASSENGERS_OUT_TOOLTIP Hành khách xuống xe tại đây.
STOP_PANEL_PASSENGERS_TOTAL Tổng:
STOP_PANEL_PASSENGERS_TOTAL_TOOLTIP Tổng hành khách được phục vụ tại đây.
STOP_PANEL_UNBUNCHING_TOOLTIP Bật hoặc tắt chống dồn xe tại điểm dừng này.\nChống dồn tắt nếu độ mạnh = 0.
STOP_PANEL_UPDATE_CLOSE_STOPS Cập nhật điểm dừng gần
STOP_PANEL_UPDATE_CLOSE_STOPS_TOOLTIP Đặt trạng thái chống dồn cho mọi điểm dừng khác tại ga/vị trí này.
STOP_PANEL_PREVIOUS Điểm dừng trước
STOP_PANEL_PREVIOUS_TOOLTIP Nhảy tới điểm dừng trước.\nGiữ Shift khi nhấp để phóng to.
STOP_PANEL_DELETE_STOP Xóa điểm dừng
STOP_PANEL_DELETE_STOP_TOOLTIP Nút này bật khi giữ phím Alt.\nDùng với rủi ro của bạn!!!
STOP_PANEL_NEXT Điểm dừng sau
STOP_PANEL_NEXT_TOOLTIP Nhảy tới điểm dừng sau.\nGiữ Shift khi nhấp để phóng to.
STOP_BUTTON_TOOLTIP {0}\n\nNhấp để nhảy tới điểm dừng này.\nGiữ Shift khi nhấp để phóng to.\nGiữ Alt khi nhấp để không mở bảng thông tin điểm dừng.
SETTINGS_DELETE Xóa
SETTINGS_RESET Đặt lại
SETTINGS_TAB_GENERAL Chung
SETTINGS_ADVANCED_LINKS_GROUP Liên kết
SETTINGS_GITHUB_REPO Mã nguồn trên GitHub
SETTINGS_TAB_AUTOLINE Tuyến tự động
SETTINGS_TAB_STOPS Điểm dừng và ga
SETTINGS_TAB_UNBUNCHING Chống dồn xe
SETTINGS_TAB_DELETE Xóa tuyến
SETTINGS_TAB_FLEET Đội xe & lịch
SETTINGS_TAB_BUDGET Ngân sách & giá
SETTINGS_TAB_LINECOLORS Màu tuyến
SETTINGS Cài đặt
SETTINGS_SPEED Hiện tốc độ bằng: 
SETTINGS_SPEED_TOOLTIP Chọn đơn vị hiển thị tốc độ trong giao diện.
SETTINGS_GAMEPLAY_PROFILE Hồ sơ gameplay
SETTINGS_GAMEPLAY_PROFILE_TOOLTIP Áp một gói cài đặt cùng lúc. An toàn (mặc định) tắt hết để tương thích tối đa với mod khác. Vanilla giống game gốc. Khuyên dùng bật lõi IPT (điều khiển đội xe theo ngân sách, chống dồn, liên tỉnh, tab công trình con, unstucker, chọn điểm dừng nâng cao, điểm dừng trên cao). Thực tế bật hầu hết tích hợp. Tùy chỉnh không tự áp hàng loạt — bạn tự bật từng mục.
SETTINGS_GAMEPLAY_PROFILE_CUSTOM Tùy chỉnh
SETTINGS_GAMEPLAY_PROFILE_SAFE An toàn (tắt hết)
SETTINGS_GAMEPLAY_PROFILE_VANILLA Vanilla
SETTINGS_GAMEPLAY_PROFILE_RECOMMENDED Khuyên dùng (lõi IPT)
SETTINGS_GAMEPLAY_PROFILE_REALISTIC Thực tế
SETTINGS_SPEED_KPH km/h
SETTINGS_SPEED_MPH mph
SETTINGS_WALKING_SPEED Tốc độ đi bộ/xe đạp: 
SETTINGS_WALKING_SPEED_TOOLTIP Tiêu chuẩn: tốc độ game gốc.\nThực tế: giảm tốc độ đi bộ theo tuổi thực tế.\nThực tế cũng giảm tốc độ xe đạp với DLC After Dark.
SETTINGS_WALKING_SPEED_MODE_VANILLA Tiêu chuẩn
SETTINGS_WALKING_SPEED_MODE_REALISTIC Thực tế
SETTINGS_BBSP Vị trí điểm dừng xe buýt tốt hơn: 
SETTINGS_BBSP_TOOLTIP Tắt: không chỉnh vị trí.\nBật: xe buýt dừng ở phía trước điểm dừng thay vì giữa.
SETTINGS_BBSP_MODE_DISABLED Tắt
SETTINGS_BBSP_MODE_ORIGINAL Bật
SETTINGS_BBSP_MODE_UPDATED Dùng logic thử nghiệm
SETTINGS_BUDGET Ngân sách
SETTINGS_ENABLE_BUDGET_CONTROL Kiểm soát ngân sách tuyến:
SETTINGS_BUDGET_CONTROL_DISABLED Tắt
SETTINGS_BUDGET_CONTROL_ENABLED Bật
SETTINGS_BUDGET_CONTROL_TOOLTIP Khi bật, số xe trên tuyến do ngân sách điều khiển; cập nhật mọi tuyến hiện có và xóa xe trong hàng đợi.
SETTINGS_BUDGET_TICKET_PRICES Tùy chỉnh giá vé:
SETTINGS_BUDGET_TICKET_PRICES_DISABLED Tắt
SETTINGS_BUDGET_TICKET_PRICES_ENABLED Bật
SETTINGS_BUDGET_TICKET_PRICES_TOOLTIP Khi bật, thêm tab mới vào bảng Kinh tế với thanh trượt giá vé cho từng loại vận tải.
SETTINGS_AUTO_LINE_BUDGET Tự điều chỉnh quy mô đội xe:
SETTINGS_AUTO_LINE_BUDGET_DISABLED Tắt
SETTINGS_AUTO_LINE_BUDGET_ENABLED Bật
SETTINGS_AUTO_LINE_BUDGET_TOOLTIP Khi bật, tuyến ở chế độ Ngân sách tự điều chỉnh số xe theo nhu cầu hành khách thực, không dùng thanh ngân sách vanilla. Tuyến Thủ công không bị đụng tới.
SETTINGS_AUTO_LINE Tuyến tự động
SETTINGS_AUTOSHOW_LINE_INFO Tự mở bảng thông tin tuyến
SETTINGS_AUTOSHOW_LINE_INFO_TOOLTIP Tự hiện bảng thông tin tuyến sau khi tạo tuyến mới.
AUTOLINECOLOR_STRATEGY_DISABLED Tắt
AUTOLINECOLOR_STRATEGY_RANDOM_HUE Hue ngẫu nhiên
AUTOLINECOLOR_STRATEGY_RANDOM_COLOR Màu ngẫu nhiên
AUTOLINECOLOR_STRATEGY_CATEGORISED Theo loại
AUTOLINECOLOR_STRATEGY_NAMED Màu có tên
AUTOLINECOLOR_NAMING_DISABLED Tắt
AUTOLINECOLOR_NAMING_DISTRICTS Quận
AUTOLINECOLOR_NAMING_LONDON London
AUTOLINECOLOR_NAMING_ROADS Đường
AUTOLINECOLOR_NAMING_COLORS Màu có tên
AUTOLINECOLOR_COLOR_STRATEGY Chiến lược màu:
AUTOLINECOLOR_COLOR_STRATEGY_TOOLTIP Cách gán màu cho tuyến mới:\n'Hue ngẫu nhiên' = cùng độ bão hòa/độ sáng, hue khác;\n'Màu ngẫu nhiên' = RGB hoàn toàn ngẫu nhiên;\n'Theo loại' = màu theo loại xe;\n'Màu có tên' = bảng màu định sẵn.
AUTOLINECOLOR_NAMING_STRATEGY Chiến lược đặt tên:
AUTOLINECOLOR_NAMING_STRATEGY_TOOLTIP Cách gán tên cho tuyến mới:\n'Không' = không tự đặt tên;\n'Quận' = theo quận phục vụ;\n'London' = tuyến số (kiểu London Buses);\n'Đường' = theo tên đường;\n'Màu có tên' = theo tên màu.
AUTOLINECOLOR_MIN_COLOR_DIFF Chênh lệch màu tối thiểu (%):
AUTOLINECOLOR_MIN_COLOR_DIFF_TOOLTIP Phần trăm chênh lệch màu tối thiểu khi chọn màu ngẫu nhiên.
AUTOLINECOLOR_MAX_COLOR_PICK Số lần thử tối đa:
AUTOLINECOLOR_MAX_COLOR_PICK_TOOLTIP Số lần thử tối đa để chọn màu phân biệt được.
SETTINGS_UI Cài đặt giao diện
SETTINGS_VEHICLE_EDITOR_POSITION Vị trí trình sửa xe: 
SETTINGS_VEHICLE_EDITOR_POSITION_TOOLTIP Chọn bảng sửa xe hiện dưới hay bên phải màn hình.
SETTINGS_VEHICLE_EDITOR_POSITION_BOTTOM Dưới
SETTINGS_VEHICLE_EDITOR_POSITION_RIGHT Phải
SETTINGS_VEHICLE_EDITOR_HIDE Ẩn trình sửa xe
SETTINGS_VEHICLE_EDITOR_HIDE_TOOLTIP Ẩn trình sửa xe khỏi bảng xe.
SETTINGS_STOPS Điểm dừng giao thông công cộng
SETTINGS_STOPSANDSTATIONS_DESCRIPTION Bao nhiêu hành khách mỗi loại vận tải có thể chờ tại một điểm dừng trước khi coi là đầy. Giá trị cao giảm phàn nàn quá tải tại điểm đông, với hàng đợi kém thực tế hơn.
SETTINGS_STOPSANDSTATIONS_RESET_TOOLTIP Đặt lại mọi giới hạn hành khách ở trên về mặc định.
SETTINGS_ENABLE_STOPS_AND_STATIONS Bật điểm dừng và ga
SETTINGS_ENABLE_STOPS_AND_STATIONS_TOOLTIP Điều chỉnh số công dân tối đa có thể chờ giao thông công cộng tại các điểm dừng và ga. Cấu hình ở tab Điểm dừng.
SETTINGS_STOPSANDSTATIONS_ENABLE Bật điểm dừng và ga
SETTINGS_STOPSANDSTATIONS_ENABLE_TOOLTIP Bật hoặc tắt giới hạn hành khách tại điểm dừng.
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HEADER Hành khách chờ tối đa tại điểm dừng tuyến:
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS Xe buýt tuyến
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BUS_TOOLTIP Hành khách chờ tối đa tại điểm dừng xe buýt tuyến
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS Xe điện bánh lốp
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TROLLEYBUS_TOOLTIP Hành khách chờ tối đa tại điểm dừng xe điện bánh lốp
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS Xe buýt sơ tán
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_EVACUATION_BUS_TOOLTIP Hành khách chờ tối đa tại điểm dừng xe buýt sơ tán
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS Xe buýt du lịch
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TOURIST_BUS_TOOLTIP Hành khách chờ tối đa tại điểm dừng xe buýt du lịch
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM Tàu điện
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAM_TOOLTIP Hành khách chờ tối đa tại điểm dừng tàu điện
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO Metro
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_METRO_TOOLTIP Hành khách chờ tối đa tại ga metro
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN Tàu hỏa
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_TRAIN_TOOLTIP Hành khách chờ tối đa tại ga tàu
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL Monorail
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_MONORAIL_TOOLTIP Hành khách chờ tối đa tại điểm dừng monorail
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP Tàu thủy
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_SHIP_TOOLTIP Hành khách chờ tối đa tại cảng hàng
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_FERRY Phà
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_FERRY_TOOLTIP Hành khách chờ tối đa tại bến phà
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE Máy bay
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_AIRPLANE_TOOLTIP Hành khách chờ tối đa tại nhà ga máy bay
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR Cáp treo
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_CABLECAR_TOOLTIP Hành khách chờ tối đa tại điểm dừng cáp treo
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON Khinh khí cầu
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HOTAIRBALLOON_TOOLTIP Hành khách chờ tối đa tại điểm dừng khinh khí cầu
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER Trực thăng
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_HELICOPTER_TOOLTIP Hành khách chờ tối đa tại điểm dừng trực thăng
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BLIMP Khí cầu
SETTINGS_STOPSANDSTATIONS_MAX_PASSENGERS_BLIMP_TOOLTIP Hành khách chờ tối đa tại điểm dừng khí cầu
SETTINGS_UNBUNCHING Chống dồn xe
SETTINGS_UNBUNCHING_AGGRESSION Độ mạnh chống dồn:
SETTINGS_UNBUNCHING_AGGRESSION_TOOLTIP Chống dồn xe mạnh cỡ nào?\nGiá trị: 0–52. 0 tắt chống dồn.\nGiá trị cao ảnh hưởng mạnh giao thông và có thể làm xe biến mất.
SETTINGS_VEHICLE_COUNT Xe trên tuyến mới:
SETTINGS_VEHICLE_COUNT_TOOLTIP Số xe tự thêm vào tuyến mới khi kiểm soát ngân sách tuyến tắt.
SETTINGS_SPAWN_TIME_INTERVAL Khoảng thời gian xuất hiện:
SETTINGS_SPAWN_TIME_INTERVAL_TOOLTIP Thời gian giây giữa các lần xuất hiện xe.
SETTINGS_UNBUNCHING_RESET_BUTTON_TOOLTIP Đặt lại mọi thanh trượt về mặc định.
UNBUNCHING_ENABLED Chống dồn xe
UNBUNCHING_DISABLED Chống dồn đang tắt.
UNBUNCHING_TARGET_GAP Khoảng cách mục tiêu: {0}
SETTINGS_EBS_GROUP_BUS Dịch vụ xe buýt nhanh
SETTINGS_EBS_GROUP_TRAM Dịch vụ tàu điện nhanh
SETTINGS_EBS_DROPDOWN_UNBUNCHING_MODE Xe buýt nhanh: 
SETTINGS_EBS_TOOLTIP_UNBUNCHING_MODE 'Tắt' = Xe buýt dùng chống dồn ở trên.\n'Thận trọng' = Dừng ngắn, kiểm tra hành khách, có thể rời khi trống.\n'Mạnh' = Bỏ qua điểm dừng nếu không ai chờ.
SETTINGS_EBS_ENABLE_SELFBAL Bật tự cân bằng dịch vụ
SETTINGS_EBS_DESC_SELFBAL Cho dịch vụ xe buýt nhanh tái phân bổ xe dọc tuyến ưu tiên đoạn đông hơn và giảm thời gian chờ.
SETTINGS_EBS_TOOLTIP_SELFBAL Phân tích đoạn tuyến và có thể chuyển xe tới đoạn đông hơn hoặc điểm cuối để đều dịch vụ và giảm chờ.\nQuyết định mang tính xác suất, phụ thuộc số hành khách và tỷ lệ tái phân bổ.
SETTINGS_EBS_ENABLE_SELFBAL_TARGETMID Bật tự cân bằng tới điểm giữa tuyến
SETTINGS_EBS_DESC_SELFBAL_TARGETMID Cho phép tự cân bằng chọn điểm dừng giữa tuyến đông đúc thay vì chỉ điểm cuối khi chuyển xe.
SETTINGS_EBS_TOOLTIP_SELFBAL_TARGETMID Cho bộ tự cân bằng chuyển xe tới điểm giữa tuyến đông (thay vì điểm cuối).\nChỉ xét khi điểm đông nhất có hơn 30 người chờ, rồi chọn với ~50% và phụ thuộc tỷ lệ tái phân bổ chung.
SETTINGS_EBS_ENABLE_MINIBUS Bật chế độ xe buýt nhỏ
SETTINGS_EBS_DESC_MINIBUS Xe nhỏ rời sớm hơn khi chỉ vài người lên/xuống.
SETTINGS_EBS_TOOLTIP_MINIBUS Xe sức chứa ≤20 có thể rời sớm khi số lên + xuống ≤5.
SETTINGS_EBS_DROPDOWN_TRAM_UNBUNCHING_MODE Tàu điện nhanh: 
SETTINGS_EBS_TOOLTIP_TRAM_UNBUNCHING 'Tắt' = Tàu điện dùng chống dồn ở trên.\n'Đường sắt nhẹ' = Dừng mọi điểm, luôn chờ đủ timer (kỷ luật chặt).\n'Tàu điện thật' = Chỉ dừng khi có người lên/xuống.
SETTINGS_EBS_MODE_NONE Tắt
SETTINGS_EBS_MODE_AGGRESSIVE Mạnh
SETTINGS_EBS_MODE_PRUDENTIAL Thận trọng
SETTINGS_EBS_TRAM_MODE_LIGHT_RAIL Chế độ đường sắt nhẹ
SETTINGS_EBS_TRAM_MODE_NONE Tắt
SETTINGS_EBS_TRAM_MODE_TRAM Chế độ tàu điện thật
SETTINGS_PTU_GROUP Gỡ kẹt giao thông công cộng
SETTINGS_PTU_ENABLE Xóa hành khách bị kẹt
SETTINGS_PTU_TOOLTIP Tự xóa hành khách kẹt khi lên xe, để xe rời bình thường và tránh khởi hành đóng băng.
SETTINGS_LINE_DELETION_TOOL Công cụ xóa tuyến
SETTINGS_LINE_DELETION_TOOL_DESCRIPTION Chọn loại vận tải bên dưới, rồi nhấn Xóa để gỡ mọi tuyến loại đó khỏi thành phố hiện tại. Lựa chọn tạm thời - luôn bắt đầu bỏ chọn và tự xóa sau khi xóa; không lưu thành cài đặt.
SETTINGS_LINE_DELETION_TOOL_BUTTON_TOOLTIP Xóa mọi tuyến loại đã chọn. Chỉ hoạt động khi thành phố đã tải.
SETTINGS_LINE_DELETION_TOOL_CONFIRM_TITLE XÁC NHẬN XÓA TUYẾN
SETTINGS_LINE_DELETION_TOOL_CONFIRM_MSG Bạn sắp xóa mọi tuyến.\nBạn có muốn tiếp tục?
SETTINGS_DELETE_BUS_TOOLTIP Xóa mọi tuyến xe buýt thường.
SETTINGS_DELETE_SIGHTSEEING_BUS_LABEL Xe buýt du lịch
SETTINGS_DELETE_SIGHTSEEING_BUS_TOOLTIP Xóa mọi tuyến xe buýt du lịch.
SETTINGS_DELETE_TRAM_TOOLTIP Xóa mọi tuyến tàu điện.
SETTINGS_DELETE_TROLLEYBUS_TOOLTIP Xóa mọi tuyến xe điện bánh lốp.
SETTINGS_DELETE_TRAIN_TOOLTIP Xóa mọi tuyến tàu hỏa.
SETTINGS_DELETE_METRO_TOOLTIP Xóa mọi tuyến metro ngầm.
SETTINGS_DELETE_MONORAIL_TOOLTIP Xóa mọi tuyến monorail.
SETTINGS_DELETE_FERRY_LABEL Phà
SETTINGS_DELETE_SHIP_TOOLTIP Xóa mọi tuyến phà.
SETTINGS_DELETE_HELICOPTER_LABEL Trực thăng
SETTINGS_DELETE_HELICOPTER_TOOLTIP Xóa mọi tuyến trực thăng.
SETTINGS_DELETE_BLIMP_LABEL Khí cầu
SETTINGS_DELETE_BLIMP_TOOLTIP Xóa mọi tuyến khí cầu.
VEHICLE_EDITOR_TITLE Trình sửa xe
VEHICLE_EDITOR_SUB_TITLE {0} xe
VEHICLE_EDITOR_CAPACITY Sức chứa hành khách
VEHICLE_EDITOR_CAPACITY_TAXI Sức chứa chuyến
VEHICLE_EDITOR_CAPACITY_TAXI_TOOLTIP Số hành khách mỗi ca làm việc.
VEHICLE_EDITOR_MAINTENANCE Chi phí bảo trì
VEHICLE_EDITOR_MAX_SPEED Tốc độ tối đa
VEHICLE_EDITOR_ENGINE_ON_BOTH_ENDS Động cơ tàu ở cả hai đầu
VEHICLE_EDITOR_ENGINE_ON_BOTH_ENDS_TOOLTIP Bật hoặc tắt động cơ tàu ở cả hai đầu.
VEHICLE_EDITOR_APPLY Áp dụng
VEHICLE_EDITOR_DEFAULT Mặc định
VEHICLE_LIST_BOX_ROW_TOOLTIP1 Nhấp phải để theo xe này.\nGiữ Shift khi nhấp để phóng to.
VEHICLE_LIST_BOX_ROW_TOOLTIP2 Shift + nhấp để xếp xe này vào hàng đợi.
VEHICLE_PANEL_EDIT_TYPE Sửa loại xe
VEHICLE_PANEL_EDIT_TYPE_TOOLTIP Sửa loại xe này bằng trình sửa xe.
VEHICLE_PANEL_STATUS_NEXT_STOP Điểm dừng tiếp:
VEHICLE_PANEL_STATUS_UNBUNCHING Đang chống dồn
VEHICLE_PANEL_LAST_STOP_EXCHANGE Trao đổi hành khách điểm dừng cuối: <color #FF0000>-{0}</color> | <color #00FF00>+{1}</color>
VEHICLE_PANEL_PASSENGERS Hành khách:
VEHICLE_PANEL_EARNINGS Doanh thu:
VEHICLE_PANEL_EARNINGS_TOOLTIP Kết quả bán vé trừ chi phí bảo trì xe.
VEHICLE_PANEL_PREVIOUS Xe trước
VEHICLE_PANEL_PREVIOUS_TOOLTIP Nhảy tới xe trước.\nGiữ Shift khi nhấp để phóng to.
VEHICLE_PANEL_REMOVE_VEHICLE Gỡ xe
VEHICLE_PANEL_NEXT Xe sau
VEHICLE_PANEL_NEXT_TOOLTIP Nhảy tới xe sau.\nGiữ Shift khi nhấp để phóng to.
VEHICLE_SELECTION_CAPACITY Sức chứa
VEHICLE_SELECTION_ADD_VEHICLE Thêm xe này vào danh sách xe được phép
VEHICLE_SELECTION_ADD_ALL Thêm mọi xe đủ điều kiện vào danh sách xe được phép
VEHICLE_SELECTION_REMOVE_VEHICLE Gỡ xe này khỏi danh sách xe được phép
VEHICLE_SELECTION_REMOVE_ALL Gỡ mọi xe khỏi danh sách xe được phép
VEHICLE_SELECTION_AVAILABLE_VEHICLES Xe khả dụng
VEHICLE_SELECTION_SELECTED_VEHICLES Xe đã chọn
VEHICLE_SELECTION_ANY_VEHICLE Bất kỳ xe nào
VEHICLE_BUTTON_TOOLTIP {0}\n\nNhấp để nhảy tới xe này.\nGiữ Shift khi nhấp để phóng to.\nGiữ Alt khi nhấp để không mở bảng thông tin xe.
TRANSPORT_LINE_VEHICLECOUNT Số xe: {0}
FLIGHT_TRACKER_NAME Theo dõi chuyến bay
FLIGHT_STATUS_NONE Không
FLIGHT_STATUS_INCOMING Đang đến
FLIGHT_STATUS_LANDED Đã hạ cánh
FLIGHT_STATUS_AT_GATE Tại cổng
FLIGHT_STATUS_DEPARTED Đã khởi hành
TICKET_PRICE_TAXI_KILOMETER Giá taxi mỗi km: 
TICKET_PRICE_TAXI_MILE Giá taxi mỗi dặm: 
TICKET_PRICE_BUS Giá vé xe buýt: 
TICKET_PRICE_INTERCITY_BUS Giá vé xe buýt liên tỉnh: 
TICKET_PRICE_METRO Giá vé metro: 
TICKET_PRICE_TRAIN Giá vé tàu: 
TICKET_PRICE_TRAM Giá vé tàu điện: 
TICKET_PRICE_MONORAIL Giá vé monorail: 
TICKET_PRICE_SHIP Giá vé tàu thủy: 
TICKET_PRICE_FERRY Giá vé phà: 
TICKET_PRICE_PLANE Giá vé máy bay: 
TICKET_PRICE_CABLECAR Giá vé cáp treo: 
TICKET_PRICE_SIGHTSEEING_BUS Giá vé xe buýt du lịch: 
TICKET_PRICE_TROLLEYBUS Giá vé xe điện bánh lốp: 
TICKET_PRICE_BLIMP Giá vé khí cầu: 
TICKET_PRICE_HELICOPTER Giá vé trực thăng: 
ECONOMY_TAB_TICKET_PRICES Giá vé
ECONOMY_TAB_TICKET_PRICES_TOOLTIP_PASSENGER_COUNT Tổng hành khách hiện tại cho loại vận tải này.
WHATSNEW_3_0_0_1 Cập nhật cho Race Day; tương thích More Vehicles Renewed.
WHATSNEW_3_0_0_2 Các mod sau đã tích hợp vào IPT3:\n • Advanced Stop Selection Revisited\n • Auto Line Color Redux\n • Better Bus Stop Position\n • Better Train Boarding\n • Elevated Stops Enabler Revisited  \n • Express Bus Services\n • Flight Tracker\n • Intercity Bus Control\n • Mileage Taxi Services\n • Public Transport Unstucker\n • Realistic Walking Speed\n • Stops and Stations\n • Ticket Price Customizer
WHATSNEW_3_0_1 Public Transport Unstucker đã tích hợp vào IPT3.\nXem bản 3.0 cho các mod tích hợp khác. Hủy đăng ký bản gốc để tránh xung đột.
SETTINGS_TAB_TRAINDISPLAY Hiển thị tàu
SETTINGS_TAB_INTEGRATIONS Tích hợp
SETTINGS_TRAINDISPLAY_GROUP Lớp phủ hiển thị tàu
SETTINGS_TRAINDISPLAY_GROUP_DESCRIPTION Cấu hình lớp phủ tích hợp khi theo xe vận tải được hỗ trợ.
SETTINGS_TRAINDISPLAY_ENABLE Bật hiển thị tàu
SETTINGS_TRAINDISPLAY_ENABLE_TOOLTIP Bật hoặc tắt lớp phủ hiển thị tàu tích hợp.
SETTINGS_TRAINDISPLAY_MODE_DISABLED Tắt
SETTINGS_TRAINDISPLAY_MODE_ENABLED Bật
SETTINGS_TRAINDISPLAY_OVERLAY_POSITION Vị trí lớp phủ:
SETTINGS_TRAINDISPLAY_OVERLAY_POSITION_TOOLTIP Chọn chỗ lớp phủ hiện trên màn hình.
SETTINGS_TRAINDISPLAY_POS_TOPLEFT Trên trái
SETTINGS_TRAINDISPLAY_POS_TOPRIGHT Trên phải
SETTINGS_TRAINDISPLAY_POS_BOTTOMLEFT Dưới trái
SETTINGS_TRAINDISPLAY_POS_BOTTOMRIGHT Dưới phải
SETTINGS_TRAINDISPLAY_OVERLAY_SCALE Tỷ lệ lớp phủ:
SETTINGS_TRAINDISPLAY_OVERLAY_SCALE_TOOLTIP Chỉnh kích thước lớp phủ.
SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY Độ mờ lớp phủ:
SETTINGS_TRAINDISPLAY_OVERLAY_OPACITY_TOOLTIP Chỉnh độ trong suốt của lớp phủ.
SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL Khoảng cập nhật:
SETTINGS_TRAINDISPLAY_UPDATE_INTERVAL_TOOLTIP Tần suất làm mới lớp phủ khi theo xe.
SETTINGS_TRAINDISPLAY_SHOW_LINE Hiện tên tuyến
SETTINGS_TRAINDISPLAY_SHOW_LINE_TOOLTIP Gồm tên tuyến trong lớp phủ.
SETTINGS_TRAINDISPLAY_SHOW_DESTINATION Hiện điểm đến
SETTINGS_TRAINDISPLAY_SHOW_DESTINATION_TOOLTIP Gồm điểm đến trong lớp phủ.
SETTINGS_TRAINDISPLAY_SHOW_STATE Hiện trạng thái
SETTINGS_TRAINDISPLAY_SHOW_STATE_TOOLTIP Gồm trạng thái xe trong lớp phủ.
SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING Chỉ khi theo
SETTINGS_TRAINDISPLAY_ONLY_WHILE_FOLLOWING_TOOLTIP Ẩn lớp phủ trừ khi camera thực sự theo xe được hỗ trợ.
SETTINGS_TRAINDISPLAY_FIRST_PERSON_ONLY Chỉ camera ngôi thứ nhất
SETTINGS_TRAINDISPLAY_FIRST_PERSON_ONLY_TOOLTIP Chỉ hiện lớp phủ khi dùng mod camera ngôi thứ nhất (vd. First Person Camera - Continued). Nếu tắt, hiện khi theo xe được hỗ trợ ở mọi chế độ camera.
SETTINGS_TRAINDISPLAY_THEME Chủ đề màu:
SETTINGS_TRAINDISPLAY_THEME_TOOLTIP Chọn màu nền/chữ của lớp phủ.
SETTINGS_TRAINDISPLAY_THEME_SIMPLE Đơn giản
SETTINGS_TRAINDISPLAY_THEME_DARK Tối
SETTINGS_TRAINDISPLAY_THEME_LIGHT Sáng
SETTINGS_TRAINDISPLAY_THEME_ORIGINAL Gốc
SETTINGS_TRAINDISPLAY_THEME_BLUE Xanh dương
SETTINGS_TRAINDISPLAY_THEME_GREEN Xanh lá
SETTINGS_TRAINDISPLAY_THEME_AMBER Hổ phách
COPY_TIP Sao chép cài đặt tuyến này.
PASTE_TIP Dán cài đặt tuyến đã sao chép.
COPY_BUILDING_TIP Sao chép các cài đặt này sang mọi tuyến phục vụ tòa nhà này.
COPY_DISTRICT_TIP Sao chép các cài đặt này sang mọi tuyến trong quận này.
SETTINGS_INTEGRATIONS_GROUP Tiện ích tích hợp
SETTINGS_INTERCITY_BUS_ENABLE Bật điều khiển xe buýt liên tỉnh
SETTINGS_INTERCITY_BUS_ENABLE_TOOLTIP Bật lớp tương thích điều khiển xe buýt liên tỉnh và vá ga.
SETTINGS_ADVANCEDSTOPSELECTION_ENABLE Bật chọn điểm dừng nâng cao
SETTINGS_ADVANCEDSTOPSELECTION_ENABLE_TOOLTIP Cho phép đặt điểm dừng trên ke/đường ray thay thế của ga nhiều ray (giữ phím chế độ thay thế khi đặt). Có hiệu lực khi tải cấp tiếp theo.
SETTINGS_BETTERBOARDING_ENABLE Bật lên xe tốt hơn
SETTINGS_BETTERBOARDING_ENABLE_TOOLTIP Cải thiện quyết định lên xe để hành khách ưu tiên xe thực sự phục vụ điểm đến. Có hiệu lực khi tải cấp tiếp theo.
SETTINGS_MILEAGETAXI_ENABLE Bật taxi theo quãng đường
SETTINGS_MILEAGETAXI_ENABLE_TOOLTIP Tính cước taxi theo quãng đường thay vì cố định, chuyến dài thu nhiều hơn. Cần DLC After Dark. Có hiệu lực khi tải cấp tiếp theo.
SETTINGS_ELEVATEDSTOPS_ENABLE Bật điểm dừng trên cao
SETTINGS_ELEVATEDSTOPS_ENABLE_TOOLTIP Cho phép điểm dừng GTVT trên đường/cầu cao và giữ đèn đường trên các đoạn đó. Có hiệu lực khi tải cấp tiếp theo.
SETTINGS_INTERCITY_BUS_CAPACITY Sức chứa bến liên tỉnh
SETTINGS_INTERCITY_BUS_CAPACITY_TOOLTIP Bến xe buýt liên tỉnh chứa được bao nhiêu xe cùng lúc.
SETTINGS_TRAM_DEPOT_CAPACITY Sức chứa depot tàu điện
SETTINGS_TRAM_DEPOT_CAPACITY_TOOLTIP Vanilla đặt mỗi depot tàu điện giới hạn thực tế 100.000 xe. Thực tế và Trung bình áp trần cố định vừa; Tắt giữ hành vi hiện có.
SETTINGS_TAXI_DEPOT_CAPACITY Sức chứa depot taxi
SETTINGS_TAXI_DEPOT_CAPACITY_TOOLTIP Vanilla đặt mỗi depot taxi giới hạn thực tế 100.000 xe. Thực tế và Trung bình áp trần cố định vừa; Tắt giữ hành vi hiện có.
SETTINGS_BUS_DEPOT_CAPACITY Sức chứa depot xe buýt
SETTINGS_BUS_DEPOT_CAPACITY_TOOLTIP Vanilla đặt mỗi depot xe buýt (thường, sinh học và gara du lịch) giới hạn thực tế 100.000 xe. Thực tế và Trung bình áp trần cố định vừa; Tắt giữ hành vi hiện có.
SETTINGS_TROLLEYBUS_DEPOT_CAPACITY Sức chứa depot xe điện bánh lốp
SETTINGS_TROLLEYBUS_DEPOT_CAPACITY_TOOLTIP Vanilla đặt mỗi depot xe điện bánh lốp giới hạn thực tế 100.000 xe. Thực tế và Trung bình áp trần cố định vừa; Tắt giữ hành vi hiện có.
SETTINGS_FERRY_DEPOT_CAPACITY Sức chứa depot phà
SETTINGS_FERRY_DEPOT_CAPACITY_TOOLTIP Vanilla đặt mỗi depot phà giới hạn thực tế 100.000 xe. Thực tế và Trung bình áp trần cố định vừa; Tắt giữ hành vi hiện có.
SETTINGS_DEPOT_CAPACITY_DISABLED Tắt (không giới hạn)
SETTINGS_DEPOT_CAPACITY_INTERMEDIATE Trung bình
SETTINGS_DEPOT_CAPACITY_REALISTIC Thực tế
SETTINGS_FLIGHTTRACKER_ENABLE Bật theo dõi chuyến bay
SETTINGS_FLIGHTTRACKER_ENABLE_TOOLTIP Bật vá và hỗ trợ UI theo dõi chuyến bay tích hợp.
SETTINGS_SUBBUILDINGSTABS_ENABLE Bật tab công trình con
SETTINGS_SUBBUILDINGSTABS_ENABLE_TOOLTIP Hiện dải tab trên bảng thông tin tòa nhà khi có công trình con (vd. sân bay kèm ga metro), để chuyển giữa chúng.
SETTINGS_TAXISTANDFIX_ENABLE Bật sửa điểm đỗ taxi
SETTINGS_TAXISTANDFIX_ENABLE_TOOLTIP Gửi taxi nhàn rỗi tới điểm đỗ gần nhất thay vì lang thang ngẫu nhiên. Cần DLC After Dark.
SETTINGS_SHAREDSTOPENABLER_ENABLE Bật điểm dừng dùng chung
SETTINGS_SHAREDSTOPENABLER_ENABLE_TOOLTIP Cho phép hơn một loại vận tải (xe buýt, tàu điện, xe điện bánh lốp) dừng cùng đoạn đường. Mặc định tắt - xem GitHub mod về phần phiên bản rút gọn bỏ đi.
SETTINGS_COMMUTERDESTINATION_ENABLE Bật điểm đến hành khách (đang thiết kế lại)
SETTINGS_COMMUTERDESTINATION_ENABLE_TOOLTIP Tạm không dùng: bảng cũ trùng UI thông tin điểm dừng và bị buộc tắt đến khi thiết kế lại. Ô chọn không bật lại được.
SETTINGS_OOC_ENABLE Kết nối ngoài tối ưu
SETTINGS_OOC_ENABLE_TOOLTIP Tàu hàng, máy bay và tàu thủy chờ lâu hơn để đầy tải trước khi rời kết nối ngoài.
SETTINGS_OOC_WAIT_MULTIPLIER Hệ số chờ
SETTINGS_OOC_WAIT_MULTIPLIER_TOOLTIP Chờ lâu hơn vanilla bao nhiêu để đầy tải. Cao hơn nghĩa là ít chuyến hơn nhưng đầy hơn.
SETTINGS_OOC_PASSENGER_SCOPE Phạm vi chờ hành khách
SETTINGS_OOC_PASSENGER_SCOPE_TOOLTIP Hệ số chờ trên áp dụng ở đâu với công dân chờ GTVT. Chỉ kết nối ngoài chỉ ảnh hưởng người chờ tại kết nối ngoài; toàn thành cũng làm chậm chờ nội địa thường cho mọi công dân (khớp hành vi thực của mod nguồn).
SETTINGS_OOC_PASSENGER_SCOPE_OUTSIDE Chỉ kết nối ngoài
SETTINGS_OOC_PASSENGER_SCOPE_CITYWIDE Toàn thành
SETTINGS_OOC_PASSENGER_SCOPE_DISABLED Tắt (vanilla)
SETTINGS_OOC_DISABLE_DUMMY Tắt giao thông trang trí đi qua
SETTINGS_OOC_DISABLE_DUMMY_ROAD Tắt giao thông đường trang trí
SETTINGS_OOC_DISABLE_DUMMY_TRAIN Tắt giao thông tàu trang trí
SETTINGS_OOC_DISABLE_DUMMY_PLANE Tắt giao thông máy bay trang trí
SETTINGS_OOC_DISABLE_DUMMY_SHIP Tắt giao thông tàu thủy trang trí
SETTINGS_OOC_DISABLE_DUMMY_TOOLTIP Kết nối ngoài thường sinh thêm giao thông trang trí không thực sự vào/ra thành, chỉ cho không khí. Tắt sẽ gỡ giao thông đó mà không đụng luồng nhập/xuất/hành khách thật.
SETTINGS_UOC_ENABLE Kết nối ngoài không giới hạn
SETTINGS_UOC_ENABLE_TOOLTIP Gỡ giới hạn vanilla 4 kết nối cho đường, ray, đường tàu và bay, và nối ngược tuyến khi xây kết nối mới gần ga sẵn có. Có hiệu lực khi tải cấp tiếp theo.
SETTINGS_STTAI_ENABLE Tránh va chạm tàu ray đơn
SETTINGS_STTAI_ENABLE_TOOLTIP Giữ đoạn ray đơn cho một tàu một lúc, giữ tàu đối diện ở cửa vào đến khi đoạn trống. Tính năng gốc IPT4, không có trong vanilla hay mod hấp thụ.
SETTINGS_STOPSTACKER_ENABLE Xếp chỗ điểm dừng xe buýt
SETTINGS_STOPSTACKER_ENABLE_TOOLTIP Cho xe thứ 2/3 đến cùng điểm dùng chỗ riêng phía sau trên làn dừng thay vì xếp một hàng sau xe dẫn đầu, để nhiều xe lên/xuống cùng lúc. Tính năng gốc IPT4 (tái hiện clean-room đơn giản), không có trong vanilla hay mod hấp thụ.
TRAINDISPLAY_LABEL_NAME Tên
TRAINDISPLAY_LABEL_STATUS Trạng thái
TRAINDISPLAY_NO_LINE Không có tuyến
TRAINDISPLAY_NO_DESTINATION Không điểm đến
TRAINDISPLAY_HIDDEN Ẩn
TRAINDISPLAY_VEHICLE Phương tiện
TRAINDISPLAY_STATE_RETURNING Đang về
TRAINDISPLAY_STATE_STOPPED Tại điểm dừng
TRAINDISPLAY_STATE_EN_ROUTE Đang đi
TRAINDISPLAY_STATE_ON_LINE Trên tuyến
TRAINDISPLAY_STATE_IDLE Nhàn rỗi
AUTOLINECOLOR_REFRESH_BUTTON Làm mới tên/màu
AUTOLINECOLOR_REFRESH_BUTTON_TOOLTIP Gán lại tên và màu tuyến theo cài đặt AutoLineColor hiện tại.
AUTOLINECOLOR_REFRESH_DISABLED_TOOLTIP Bật chiến lược màu hoặc đặt tên trong Cài đặt trước khi làm mới tuyến này.
TICKET_PRICE_LABEL_TOOLTIP Giá hiện tại: {0}\nGiá gốc: {1}\nHành khách đang đi: {2}
"""

for line in _VI_LINES.strip().splitlines():
    if not line.strip():
        continue
    i = line.find(" ")
    if i > 0:
        VI[line[:i]] = line[i + 1 :]


if __name__ == "__main__":
    from lang_packs_all import emit

    emit("vi", VI)
