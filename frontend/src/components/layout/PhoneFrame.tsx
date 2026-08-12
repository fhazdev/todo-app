import type { ReactNode } from 'react'

/**
 * The design is a 412 x 892 Android screen. On a phone this is just the viewport;
 * on a desktop browser the app sits in a centred column of the same width rather
 * than stretching a phone layout across 1400px.
 *
 * The mock device bezel from the prototype is deliberately not reproduced.
 */
export function PhoneFrame({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-dvh justify-center bg-neutral-200">
      <div className="relative flex min-h-dvh w-full max-w-[412px] flex-col overflow-hidden bg-ground shadow-organic-md">
        {children}
      </div>
    </div>
  )
}
