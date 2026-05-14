import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useLocation } from "react-router";

import {
  ChevronDownIcon,
  GridIcon,
  HorizontaLDots,
  ListIcon,
  UserCircleIcon,
} from "../icons";
import { useSidebar } from "../context/SidebarContext";
import { useAuth } from "../context/AuthContext";

type NavItem = {
  name: string;
  icon: React.ReactNode;
  path?: string;
  roles?: string[];
  subItems?: { name: string; path: string; pro?: boolean; new?: boolean; roles?: string[] }[];
};

const navItems: NavItem[] = [
  {
    icon: <GridIcon />,
    name: "Tổng quan",
    subItems: [{ name: "Trang chủ", path: "/", pro: false }],
  },
  {
    icon: <UserCircleIcon />,
    name: "Hồ sơ",
    path: "/profile",
  },
  {
    name: "Vận hành bãi xe",
    icon: <ListIcon />,
    subItems: [
      { name: "Xe vào", path: "/parking/check-in", pro: false },
      { name: "Xe ra", path: "/parking/check-out", pro: false },
      { name: "Camera khu vực", path: "/parking/zone-cameras", pro: false },
    ].map((item) => ({ ...item, roles: ["Staff", "Admin"] })),
  },
  {
    name: "Quản trị",
    icon: <UserCircleIcon />,
    roles: ["Admin"],
    subItems: [
      { name: "Người dùng", path: "/admin/users", pro: false },
      { name: "Nhân viên", path: "/admin/staff", pro: false },
      { name: "Xe đã đăng ký", path: "/admin/vehicles", pro: false },
      { name: "Cấu trúc bãi", path: "/admin/parking-structure", pro: false },
      { name: "Vé tháng", path: "/admin/monthly-passes", pro: false, roles: ["Admin"] },
    ],
  },
];

const AppSidebar: React.FC = () => {
  const { isExpanded, isMobileOpen, isHovered, setIsHovered } = useSidebar();
  const { user } = useAuth();
  const location = useLocation();
  const userRoles = user?.roles || user?.Roles || (user?.role ? [user.role] : []);
  const canShow = (roles?: string[]) =>
    !roles || roles.some((role) => userRoles.includes(role));

  // Chỉ quản lý index của menu đang mở
  const [openSubmenu, setOpenSubmenu] = useState<number | null>(null);
  const [subMenuHeight, setSubMenuHeight] = useState<Record<number, number>>({});
  const subMenuRefs = useRef<Record<number, HTMLDivElement | null>>({});

  const isActive = useCallback(
    (path: string) => location.pathname === path,
    [location.pathname]
  );

  // Tự động mở submenu khi đường dẫn (URL) khớp với subItem
  useEffect(() => {
    let submenuMatched = false;
    navItems.filter((nav) => canShow(nav.roles)).forEach((nav, index) => {
      if (nav.subItems) {
        nav.subItems.filter((subItem) => canShow(subItem.roles)).forEach((subItem) => {
          if (isActive(subItem.path)) {
            setOpenSubmenu(index);
            submenuMatched = true;
          }
        });
      }
    });

    if (!submenuMatched) {
      setOpenSubmenu(null);
    }
  }, [location, isActive, userRoles.join("|")]);

  // Tính toán chiều cao để tạo hiệu ứng mượt khi đóng/mở submenu
  useEffect(() => {
    if (openSubmenu !== null && subMenuRefs.current[openSubmenu]) {
      setSubMenuHeight((prev) => ({
        ...prev,
        [openSubmenu]: subMenuRefs.current[openSubmenu]?.scrollHeight || 0,
      }));
    }
  }, [openSubmenu]);

  const handleSubmenuToggle = (index: number) => {
    setOpenSubmenu((prev) => (prev === index ? null : index));
  };

  return (
    <aside
      className={`fixed mt-16 flex flex-col lg:mt-0 top-0 px-5 left-0 bg-white dark:bg-gray-900 dark:border-gray-800 text-gray-900 h-screen transition-all duration-300 ease-in-out z-50 border-r border-gray-200 
        ${isExpanded || isMobileOpen || isHovered ? "w-[290px]" : "w-[90px]"}
        ${isMobileOpen ? "translate-x-0" : "-translate-x-full"}
        lg:translate-x-0`}
      onMouseEnter={() => !isExpanded && setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
    >
      {/* Logo Section */}
      <div className={`py-8 flex ${!isExpanded && !isHovered ? "lg:justify-center" : "justify-start"}`}>
        <Link to="/">
          {isExpanded || isHovered || isMobileOpen ? (
            <>
              <img className="dark:hidden" src="/images/logo/logo.svg" alt="Logo" width={150} height={40} />
              <img className="hidden dark:block" src="/images/logo/logo-dark.svg" alt="Logo" width={150} height={40} />
            </>
          ) : (
            <img src="/images/logo/logo-icon.svg" alt="Logo" width={32} height={32} />
          )}
        </Link>
      </div>

      <div className="flex flex-col overflow-y-auto duration-300 ease-linear no-scrollbar">
        <nav className="mb-6">
          <div className="flex flex-col gap-4">
            {/* Menu Label */}
            <h2 className={`mb-4 text-xs uppercase flex leading-[20px] text-gray-400 ${!isExpanded && !isHovered ? "lg:justify-center" : "justify-start"}`}>
              {isExpanded || isHovered || isMobileOpen ? "Danh mục" : <HorizontaLDots className="size-6" />}
            </h2>

            {/* Render Nav Items */}
            <ul className="flex flex-col gap-4">
              {navItems
                .filter((nav) => canShow(nav.roles))
                .map((nav, index) => {
                  const subItems = nav.subItems?.filter((subItem) => canShow(subItem.roles));
                  if (nav.subItems && (!subItems || subItems.length === 0)) {
                    return null;
                  }

                  return (
                <li key={nav.name}>
                  {nav.subItems ? (
                    <>
                      <button
                        onClick={() => handleSubmenuToggle(index)}
                        className={`menu-item group ${openSubmenu === index ? "menu-item-active" : "menu-item-inactive"} cursor-pointer ${!isExpanded && !isHovered ? "lg:justify-center" : "lg:justify-start"}`}
                      >
                        <span className={`menu-item-icon-size ${openSubmenu === index ? "menu-item-icon-active" : "menu-item-icon-inactive"}`}>
                          {nav.icon}
                        </span>
                        {(isExpanded || isHovered || isMobileOpen) && (
                          <>
                            <span className="menu-item-text">{nav.name}</span>
                            <ChevronDownIcon className={`ml-auto w-5 h-5 transition-transform duration-200 ${openSubmenu === index ? "rotate-180 text-brand-500" : ""}`} />
                          </>
                        )}
                      </button>

                      {/* Submenu List */}
                      {(isExpanded || isHovered || isMobileOpen) && (
                        <div
                          ref={(el) => { subMenuRefs.current[index] = el; }}
                          className="overflow-hidden transition-all duration-300"
                          style={{ height: openSubmenu === index ? `${subMenuHeight[index]}px` : "0px" }}
                        >
                          <ul className="mt-2 space-y-1 ml-9">
                            {subItems?.map((subItem) => (
                              <li key={subItem.name}>
                                <Link
                                  to={subItem.path}
                                  className={`menu-dropdown-item ${isActive(subItem.path) ? "menu-dropdown-item-active" : "menu-dropdown-item-inactive"}`}
                                >
                                  {subItem.name}
                                </Link>
                              </li>
                            ))}
                          </ul>
                        </div>
                      )}
                    </>
                  ) : (
                    nav.path && (
                      <Link
                        to={nav.path}
                        className={`menu-item group ${isActive(nav.path) ? "menu-item-active" : "menu-item-inactive"}`}
                      >
                        <span className={`menu-item-icon-size ${isActive(nav.path) ? "menu-item-icon-active" : "menu-item-icon-inactive"}`}>
                          {nav.icon}
                        </span>
                        {(isExpanded || isHovered || isMobileOpen) && (
                          <span className="menu-item-text">{nav.name}</span>
                        )}
                      </Link>
                    )
                  )}
                </li>
                  );
                })}
            </ul>
          </div>
        </nav>
        
       
      </div>
    </aside>
  );
};

export default AppSidebar;
