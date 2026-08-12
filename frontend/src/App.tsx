import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom'
import { useAuth } from '@/auth/useAuth'
import { PhoneFrame } from '@/components/layout/PhoneFrame'
import { TabBar } from '@/components/layout/TabBar'
import { OfflineBanner } from '@/components/ui/States'
import { useOnline } from '@/lib/useOnline'
import { SignInScreen } from '@/screens/SignInScreen'
import { ListsHomeScreen } from '@/screens/ListsHomeScreen'
import { ListDetailScreen } from '@/screens/ListDetailScreen'
import { NewListScreen } from '@/screens/NewListScreen'
import { ListTypesScreen } from '@/screens/ListTypesScreen'
import { TypeCategoriesScreen } from '@/screens/TypeCategoriesScreen'
import { SharedWithScreen } from '@/screens/SharedWithScreen'
import { TodayScreen } from '@/screens/TodayScreen'

/** Sends anonymous visitors to sign-in, remembering where they were headed. */
function RequireAuth() {
  const { user, isRestoring } = useAuth()
  const location = useLocation()

  // Waiting matters: without it a reload would bounce a signed-in user to the
  // sign-in screen while the stored refresh token is still being spent.
  if (isRestoring) {
    return <div className="flex flex-1 items-center justify-center text-ink/45">…</div>
  }

  return user ? (
    <Outlet />
  ) : (
    <Navigate to="/signin" replace state={{ from: location.pathname }} />
  )
}

/**
 * The three tab destinations. Pushed screens (list detail, new list, categories,
 * sharing) deliberately sit outside this layout: the design has no tab bar on
 * them, and covering the FAB with one would be wrong.
 */
function TabLayout() {
  return (
    <>
      <Outlet />
      <TabBar />
    </>
  )
}

export function App() {
  const online = useOnline()

  return (
    <PhoneFrame>
      <OfflineBanner online={online} />

      <Routes>
        <Route path="/signin" element={<SignInScreen />} />

        <Route element={<RequireAuth />}>
          <Route element={<TabLayout />}>
            <Route path="/lists" element={<ListsHomeScreen />} />
            <Route path="/today" element={<TodayScreen />} />
            <Route path="/types" element={<ListTypesScreen />} />
          </Route>

          <Route path="/lists/new" element={<NewListScreen />} />
          <Route path="/lists/:listId" element={<ListDetailScreen />} />
          <Route path="/lists/:listId/members" element={<SharedWithScreen />} />
          <Route path="/types/:listTypeId" element={<TypeCategoriesScreen />} />
        </Route>

        <Route path="*" element={<Navigate to="/lists" replace />} />
      </Routes>
    </PhoneFrame>
  )
}
